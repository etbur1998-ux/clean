import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../services/auth_service.dart';
import '../theme/app_colors.dart';
import '../theme/theme_provider.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});
  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  bool _notifications = true;
  bool _offlineSync   = false;
  String _language    = 'English';

  @override
  Widget build(BuildContext context) {
    final user          = Provider.of<AuthService>(context).currentUser;
    final themeProvider = Provider.of<ThemeProvider>(context);
    if (user == null) return const SizedBox.shrink();

    final isDark = Theme.of(context).brightness == Brightness.dark;
    final bg     = isDark ? const Color(0xFF111A19) : AppColors.background;

    return Scaffold(
      backgroundColor: bg,
      body: CustomScrollView(
        slivers: [
          // ── Teal header ───────────────────────────────────────────
          SliverToBoxAdapter(
            child: Container(
              padding: EdgeInsets.fromLTRB(
                  24, MediaQuery.of(context).padding.top + 20, 24, 32),
              decoration: const BoxDecoration(
                gradient: AppColors.cardGradient,
                borderRadius: BorderRadius.vertical(bottom: Radius.circular(32)),
              ),
              child: Column(children: [
                // Avatar
                Container(
                  padding: const EdgeInsets.all(3),
                  decoration: const BoxDecoration(
                      color: Colors.white24, shape: BoxShape.circle),
                  child: CircleAvatar(
                    radius: 44,
                    backgroundColor: Colors.white.withValues(alpha: 0.2),
                    child: const Icon(Icons.person_rounded,
                        size: 44, color: Colors.white),
                  ),
                ).animate().scale(duration: 350.ms, curve: Curves.easeOutBack),
                const SizedBox(height: 12),
                Text(user.name,
                    style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w800,
                        color: Colors.white))
                    .animate().fadeIn(delay: 100.ms),
                const SizedBox(height: 6),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 5),
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.2),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(user.role.toUpperCase(),
                      style: const TextStyle(fontSize: 11, color: Colors.white,
                          fontWeight: FontWeight.w700, letterSpacing: 1.2)),
                ).animate().fadeIn(delay: 150.ms),
              ]),
            ),
          ),

          // ── Content ───────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.all(20),
            sliver: SliverList(
              delegate: SliverChildListDelegate([
                // User info card
                _section('Account', isDark),
                _card(isDark, [
                  _infoTile(Icons.phone_iphone_rounded, 'Phone', user.phone, isDark),
                  _divider(isDark),
                  _infoTile(Icons.badge_rounded, 'Role / ID',
                      '${user.role}  •  #${user.id}', isDark),
                  if (user.isDriver && user.vehicleName != null) ...[
                    _divider(isDark),
                    _infoTile(Icons.local_shipping_rounded,
                        'Assigned Vehicle', user.vehicleName!, isDark),
                  ],
                ]).animate().slideY(begin: 0.1, duration: 250.ms),

                if (user.isDriver) ...[
                  const SizedBox(height: 20),
                  _section('Driver', isDark),
                  _card(isDark, [
                    ListTile(
                      leading: _iconBox(Icons.local_shipping_rounded,
                          AppColors.secondary, isDark),
                      title: Text('Driver Console & Jobs',
                          style: _bold(isDark)),
                      subtitle: Text('Manage transport requests',
                          style: _sub(isDark)),
                      trailing: const Icon(Icons.chevron_right_rounded,
                          color: AppColors.textHint),
                      onTap: () => context.push('/driver-trips'),
                    ),
                  ]).animate().slideY(begin: 0.1, duration: 300.ms),
                ],

                const SizedBox(height: 20),
                _section('Preferences', isDark),
                _card(isDark, [
                  // Dark mode
                  SwitchListTile(
                    value: themeProvider.isDarkMode,
                    onChanged: themeProvider.toggleTheme,
                    activeThumbColor: AppColors.primary,
                    secondary: _iconBox(Icons.dark_mode_rounded, Colors.amber, isDark),
                    title: Text('Dark Mode', style: _bold(isDark)),
                    subtitle: Text('Toggle light / dark theme', style: _sub(isDark)),
                  ),
                  _divider(isDark),
                  SwitchListTile(
                    value: _notifications,
                    onChanged: (v) => setState(() => _notifications = v),
                    activeThumbColor: AppColors.primary,
                    secondary: _iconBox(Icons.notifications_active_rounded,
                        Colors.purple, isDark),
                    title: Text('Push Notifications', style: _bold(isDark)),
                    subtitle: Text('Real-time trip alerts', style: _sub(isDark)),
                  ),
                  _divider(isDark),
                  SwitchListTile(
                    value: _offlineSync,
                    onChanged: (v) => setState(() => _offlineSync = v),
                    activeThumbColor: AppColors.primary,
                    secondary: _iconBox(Icons.cloud_sync_rounded, Colors.orange, isDark),
                    title: Text('Offline Sync', style: _bold(isDark)),
                    subtitle: Text('Save submissions locally', style: _sub(isDark)),
                  ),
                  _divider(isDark),
                  ListTile(
                    leading: _iconBox(Icons.translate_rounded, Colors.teal, isDark),
                    title: Text('Language', style: _bold(isDark)),
                    subtitle: Text(_language, style: _sub(isDark)),
                    trailing: const Icon(Icons.keyboard_arrow_down_rounded,
                        color: AppColors.textHint),
                    onTap: _languageDialog,
                  ),
                ]).animate().slideY(begin: 0.1, duration: 350.ms),

                const SizedBox(height: 20),
                _section('Legal', isDark),
                _card(isDark, [
                  ListTile(
                    leading: _iconBox(Icons.security_rounded, Colors.red, isDark),
                    title: Text('Privacy Policy', style: _bold(isDark)),
                    subtitle: Text('View data & usage terms', style: _sub(isDark)),
                    trailing: const Icon(Icons.chevron_right_rounded,
                        color: AppColors.textHint),
                    onTap: _privacyDialog,
                  ),
                  _divider(isDark),
                  ListTile(
                    leading: _iconBox(Icons.info_outline_rounded,
                        Colors.blueGrey, isDark),
                    title: Text('App Version', style: _bold(isDark)),
                    subtitle: Text('v1.0.26 • Build 12', style: _sub(isDark)),
                  ),
                ]).animate().slideY(begin: 0.1, duration: 400.ms),

                const SizedBox(height: 28),

                // Logout
                SizedBox(
                  height: 52,
                  child: OutlinedButton.icon(
                    onPressed: () {
                      Provider.of<AuthService>(context, listen: false).logout();
                    },
                    icon: const Icon(Icons.logout_rounded, color: AppColors.error),
                    label: const Text('Logout',
                        style: TextStyle(color: AppColors.error,
                            fontSize: 16, fontWeight: FontWeight.w700)),
                    style: OutlinedButton.styleFrom(
                      side: const BorderSide(color: AppColors.error, width: 1.5),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14)),
                    ),
                  ),
                ).animate().fadeIn(delay: 200.ms),

                const SizedBox(height: 80),
              ]),
            ),
          ),
        ],
      ),
    );
  }

  // ── Helpers ──────────────────────────────────────────────────────

  Widget _section(String title, bool isDark) => Padding(
    padding: const EdgeInsets.only(left: 4, bottom: 8),
    child: Text(title.toUpperCase(),
        style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700,
            letterSpacing: 1.5,
            color: isDark ? Colors.white38 : AppColors.textSecondary)),
  );

  Widget _card(bool isDark, List<Widget> children) => Container(
    decoration: BoxDecoration(
      color: isDark ? const Color(0xFF1E2D2C) : Colors.white,
      borderRadius: BorderRadius.circular(16),
      boxShadow: [BoxShadow(
          color: isDark ? Colors.black12 : AppColors.shadow,
          blurRadius: 8, offset: const Offset(0, 2))],
    ),
    child: ClipRRect(
      borderRadius: BorderRadius.circular(16),
      child: Column(children: children),
    ),
  );

  Widget _infoTile(IconData icon, String label, String value, bool isDark) =>
      ListTile(
        leading: _iconBox(icon, AppColors.primary, isDark),
        title: Text(label,
            style: TextStyle(fontSize: 11,
                color: isDark ? Colors.white38 : AppColors.textHint)),
        subtitle: Text(value,
            style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600,
                color: isDark ? Colors.white : AppColors.textPrimary)),
      );

  Widget _iconBox(IconData icon, Color color, bool isDark) => Container(
    width: 38, height: 38,
    decoration: BoxDecoration(
      color: color.withValues(alpha: isDark ? 0.15 : 0.1),
      borderRadius: BorderRadius.circular(10),
    ),
    child: Icon(icon, color: color, size: 20),
  );

  Widget _divider(bool isDark) => Divider(
      height: 1, indent: 56, endIndent: 16,
      color: isDark ? Colors.white10 : Colors.grey[200]);

  TextStyle _bold(bool isDark) => TextStyle(
      fontWeight: FontWeight.w600, fontSize: 14,
      color: isDark ? Colors.white : AppColors.textPrimary);

  TextStyle _sub(bool isDark) => TextStyle(
      fontSize: 12, color: isDark ? Colors.white38 : AppColors.textSecondary);

  void _languageDialog() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
      builder: (_) => Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(width: 36, height: 4,
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                    color: Colors.grey[300],
                    borderRadius: BorderRadius.circular(2))),
            const Text('Select Language',
                style: TextStyle(fontSize: 17, fontWeight: FontWeight.w700)),
            const SizedBox(height: 12),
            ...['English', 'Amharic', 'Oromiffa', 'Tigrinya'].map((lang) =>
              ListTile(
                title: Text(lang),
                trailing: _language == lang
                    ? const Icon(Icons.check_rounded, color: AppColors.primary)
                    : null,
                onTap: () {
                  setState(() => _language = lang);
                  Navigator.pop(context);
                },
              )),
          ],
        ),
      ),
    );
  }

  void _privacyDialog() {
    showDialog(
      context: context,
      builder: (_) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: const Row(children: [
          Icon(Icons.security_rounded, color: AppColors.primary),
          SizedBox(width: 10),
          Text('Privacy Policy'),
        ]),
        content: const SingleChildScrollView(
          child: Text(
            'CleanApp collects work data, GPS coordinates, receipt photos, and vehicle telemetry to optimize municipal cleaning workflow in Yeka sub-city. All data is encrypted in transit and stored on secure administrative servers.\n\n'
            'Drivers are responsible for accurate KG entries. False data or unrelated evidence photos are subject to disciplinary review.',
            style: TextStyle(height: 1.5),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Understood',
                style: TextStyle(fontWeight: FontWeight.w700,
                    color: AppColors.primary)),
          ),
        ],
      ),
    );
  }
}
