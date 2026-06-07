(function () {
  const languageJsonUrl = '/js/language.json';
  const storageKey = 'yekaCleaningLanguage';
  const languageLabels = {
    english: 'English',
    amharic: 'Amharic',
    oromifa: 'Oromifa',
    tigrigna: 'Tigrigna'
  };

  const state = {
    translations: null,
    currentLanguage: 'english'
  };

  function getStoredLanguage() {
    return window.localStorage.getItem(storageKey) || 'english';
  }

  function setStoredLanguage(language) {
    window.localStorage.setItem(storageKey, language);
    state.currentLanguage = language;
  }

  function getLanguageLabel(language) {
    return languageLabels[language] || language;
  }

  async function loadTranslations() {
    if (state.translations) {
      return state.translations;
    }

    try {
      const response = await fetch(languageJsonUrl, { cache: 'no-store' });
      if (!response.ok) {
        throw new Error(`Unable to load translation data: ${response.status}`);
      }
      state.translations = await response.json();
    } catch (error) {
      console.error('Translation load error:', error);
      state.translations = { english: {} };
    }

    return state.translations;
  }

  function getTranslation(language, key) {
    const translations = state.translations || {};
    const languageMap = translations[language] || translations.english || {};
    if (languageMap && key in languageMap) {
      return languageMap[key];
    }
    if (translations.english && key in translations.english) {
      return translations.english[key];
    }
    return null;
  }

  function translateElement(element, language) {
    const key = element.dataset.i18n;
    if (!key) {
      return;
    }

    const translation = getTranslation(language, key);
    if (translation == null) {
      return;
    }

    if (element.dataset.i18nHtml !== undefined) {
      element.innerHTML = translation;
    } else {
      element.textContent = translation;
    }
  }

  function translateAttributes(language) {
    document.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
      const key = el.dataset.i18nPlaceholder;
      const translation = getTranslation(language, key);
      if (translation != null) {
        el.placeholder = translation;
      }
    });

    document.querySelectorAll('[data-i18n-title]').forEach(el => {
      const key = el.dataset.i18nTitle;
      const translation = getTranslation(language, key);
      if (translation != null) {
        if (el.tagName.toLowerCase() === 'title') {
          el.textContent = translation;
        } else {
          el.title = translation;
        }
      }
    });

    document.querySelectorAll('[data-i18n-value]').forEach(el => {
      const key = el.dataset.i18nValue;
      const translation = getTranslation(language, key);
      if (translation != null) {
        el.value = translation;
      }
    });
  }

  function translatePage(language) {
    if (!state.translations) {
      return;
    }

    const effectiveLanguage = language || state.currentLanguage || getStoredLanguage();
    state.currentLanguage = effectiveLanguage;

    document.documentElement.lang = effectiveLanguage === 'english' ? 'en' : effectiveLanguage === 'amharic' ? 'am' : effectiveLanguage === 'oromifa' ? 'om' :effectiveLanguage === 'tigrigna' ? 'tr' :'en';

    document.querySelectorAll('[data-i18n]').forEach(element => translateElement(element, effectiveLanguage));
    translateAttributes(effectiveLanguage);
    updateLanguageSelector(effectiveLanguage);

    if (window.YekaTranslation && typeof window.YekaTranslation.onLanguageChanged === 'function') {
      window.YekaTranslation.onLanguageChanged(effectiveLanguage);
    }
  }

  function updateLanguageSelector(language) {
    const select = document.getElementById('languageSelect');
    if (!select) {
      return;
    }

    if (select.value !== language) {
      select.value = language;
    }
  }

  function populateLanguageSelector() {
    const select = document.getElementById('languageSelect');
    if (!select) {
      return;
    }

    select.innerHTML = '';
    if (!state.translations) {
      return;
    }

    Object.keys(state.translations).forEach(language => {
      const option = document.createElement('option');
      option.value = language;
      option.textContent = getLanguageLabel(language);
      select.appendChild(option);
    });

    const current = getStoredLanguage();
    if (state.translations[current]) {
      select.value = current;
    }

    select.addEventListener('change', event => {
      const selected = event.target.value;
      setStoredLanguage(selected);
      translatePage(selected);
    });
  }

  window.YekaTranslation = {
    get currentLanguage() {
      return state.currentLanguage;
    },
    set currentLanguage(language) {
      setStoredLanguage(language);
      translatePage(language);
    },
    get(key) {
      return getTranslation(state.currentLanguage, key);
    },
    onLanguageChanged: null,
    async init() {
      await loadTranslations();
      const current = getStoredLanguage();
      state.currentLanguage = current;
      populateLanguageSelector();
      translatePage(current);
    }
  };

  document.addEventListener('DOMContentLoaded', () => {
    window.YekaTranslation.init();
  });
})();
