import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:provider/provider.dart';
import '../services/auth_service.dart';
import '../theme/app_colors.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});
  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailCtrl = TextEditingController();
  final _passCtrl = TextEditingController();
  bool _obscure = true;
  bool _loading = false;

  final _quickLogins = [
    {
      'label': 'Driver',
      'icon': Icons.local_shipping_rounded,
      'email': 'driver1@yeka.et',
      'pass': 'driver123',
    },
    {
      'label': 'Outsource',
      'icon': Icons.business_rounded,
      'email': 'outsource@yeka.et',
      'pass': 'outsource123',
    },
    {
      'label': 'Private',
      'icon': Icons.cleaning_services_rounded,
      'email': 'private@yeka.et',
      'pass': 'private123',
    },
    {
      'label': 'Manager',
      'icon': Icons.admin_panel_settings_rounded,
      'email': 'manager@yeka.et',
      'pass': 'manager123',
    },
  ];

  Future<void> _login() async {
    if (_emailCtrl.text.trim().isEmpty || _passCtrl.text.trim().isEmpty) {
      _snack('Please enter email and password');
      return;
    }
    setState(() => _loading = true);
    final ok = await Provider.of<AuthService>(
      context,
      listen: false,
    ).login(_emailCtrl.text.trim(), _passCtrl.text.trim());
    if (mounted) {
      setState(() => _loading = false);
      if (!ok) _snack('Invalid credentials or account inactive');
    }
  }

  void _quick(String email, String pass) {
    _emailCtrl.text = email;
    _passCtrl.text = pass;
    _login();
  }

  void _snack(String msg) => ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(msg),
      behavior: SnackBarBehavior.floating,
      backgroundColor: AppColors.primaryDark,
      margin: const EdgeInsets.all(16),
    ),
  );

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final size = MediaQuery.of(context).size;
    final isWide = size.width > 600; // tablet / desktop

    return Scaffold(
      backgroundColor: const Color(0xFF00695C),
      body: Stack(
        children: [
          // Decorative circles
          Positioned(
            top: -60,
            right: -60,
            child: _circle(200, Colors.white.withValues(alpha: 0.06)),
          ),
          Positioned(
            top: 80,
            left: -80,
            child: _circle(160, Colors.white.withValues(alpha: 0.05)),
          ),
          Positioned(
            bottom: -80,
            right: -40,
            child: _circle(250, Colors.white.withValues(alpha: 0.04)),
          ),

          SafeArea(child: isWide ? _wideLayout(size) : _narrowLayout(size)),
        ],
      ),
    );
  }

  // ── Wide layout (tablet / web) ────────────────────────────────────────────
  Widget _wideLayout(Size size) => Row(
    children: [
      // Left hero panel
      Expanded(
        child: Container(
          height: double.infinity,
          padding: const EdgeInsets.all(40),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                width: 100,
                height: 100,
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(28),
                ),
                child: const Icon(
                  Icons.cleaning_services_rounded,
                  size: 60,
                  color: Colors.white,
                ),
              ).animate().scale(duration: 400.ms, curve: Curves.easeOutBack),
              const SizedBox(height: 24),
              const Text(
                'CleanApp',
                style: TextStyle(
                  fontSize: 40,
                  fontWeight: FontWeight.w900,
                  color: Colors.white,
                  letterSpacing: 0.5,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Yeka Cleaning Management System',
                style: TextStyle(
                  fontSize: 16,
                  color: Colors.white.withValues(alpha: 0.75),
                ),
              ),
              const SizedBox(height: 40),
              _statsRow(),
            ],
          ),
        ),
      ),
      // Right login card
      Container(
        width: 420,
        height: double.infinity,
        color: const Color(0xFFF0F4F3),
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(40),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [const SizedBox(height: 60), _loginFormContent()],
          ),
        ),
      ),
    ],
  );

  // ── Narrow layout (phone) ─────────────────────────────────────────────────
  Widget _narrowLayout(Size size) => SingleChildScrollView(
    child: SizedBox(
      height: size.height - MediaQuery.of(context).padding.top,
      child: Column(
        children: [
          Expanded(flex: 2, child: _heroSection()),
          Expanded(
            flex: 3,
            child:
                Container(
                  width: double.infinity,
                  decoration: const BoxDecoration(
                    color: Color(0xFFF0F4F3),
                    borderRadius: BorderRadius.vertical(
                      top: Radius.circular(32),
                    ),
                  ),
                  padding: const EdgeInsets.fromLTRB(24, 32, 24, 24),
                  child: _loginFormContent(),
                ).animate().slideY(
                  begin: 0.15,
                  duration: 350.ms,
                  curve: Curves.easeOut,
                ),
          ),
        ],
      ),
    ),
  );

  Widget _heroSection() => Column(
    mainAxisAlignment: MainAxisAlignment.center,
    children: [
      Container(
        width: 84,
        height: 84,
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: 0.15),
          borderRadius: BorderRadius.circular(24),
        ),
        child: const Icon(
          Icons.cleaning_services_rounded,
          size: 48,
          color: Colors.white,
        ),
      ).animate().scale(duration: 400.ms, curve: Curves.easeOutBack),
      const SizedBox(height: 16),
      const Text(
        'CleanApp',
        style: TextStyle(
          fontSize: 32,
          fontWeight: FontWeight.w800,
          color: Colors.white,
          letterSpacing: 0.5,
        ),
      ).animate().fadeIn(delay: 150.ms).slideY(begin: 0.3),
      const SizedBox(height: 6),
      Text(
        'Yeka Cleaning Management',
        style: TextStyle(
          fontSize: 14,
          color: Colors.white.withValues(alpha: 0.75),
        ),
      ).animate().fadeIn(delay: 200.ms),
    ],
  );

  Widget _loginFormContent() => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      const Text(
        'Sign In',
        style: TextStyle(
          fontSize: 26,
          fontWeight: FontWeight.w800,
          color: AppColors.textPrimary,
        ),
      ),
      const SizedBox(height: 4),
      const Text(
        'Enter your credentials to continue',
        style: TextStyle(color: AppColors.textSecondary, fontSize: 14),
      ),
      const SizedBox(height: 28),

      _field(
        controller: _emailCtrl,
        label: 'Email / Username',
        icon: Icons.person_outline_rounded,
        keyboard: TextInputType.emailAddress,
      ),
      const SizedBox(height: 16),
      _field(
        controller: _passCtrl,
        label: 'Password',
        icon: Icons.lock_outline_rounded,
        obscure: _obscure,
        suffix: IconButton(
          icon: Icon(
            _obscure ? Icons.visibility_off : Icons.visibility,
            color: AppColors.textHint,
            size: 20,
          ),
          onPressed: () => setState(() => _obscure = !_obscure),
        ),
        onSubmit: _login,
      ),
      const SizedBox(height: 28),

      SizedBox(
        width: double.infinity,
        height: 54,
        child: ElevatedButton(
          onPressed: _loading ? null : _login,
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            foregroundColor: Colors.white,
            elevation: 0,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14),
            ),
          ),
          child: _loading
              ? const SizedBox(
                  width: 22,
                  height: 22,
                  child: CircularProgressIndicator(
                    color: Colors.white,
                    strokeWidth: 2.5,
                  ),
                )
              : const Text(
                  'Sign In',
                  style: TextStyle(fontSize: 17, fontWeight: FontWeight.w700),
                ),
        ),
      ),

      const SizedBox(height: 24),
      Center(
        child: Text(
          'Quick Access',
          style: TextStyle(
            fontSize: 12,
            color: AppColors.textHint,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
      const SizedBox(height: 10),
      Wrap(
        alignment: WrapAlignment.center,
        spacing: 8,
        runSpacing: 8,
        children: _quickLogins
            .map(
              (q) => _quickChip(
                q['label'] as String,
                q['icon'] as IconData,
                () => _quick(q['email'] as String, q['pass'] as String),
              ),
            )
            .toList(),
      ),
    ],
  );

  Widget _statsRow() => Row(
    mainAxisAlignment: MainAxisAlignment.spaceEvenly,
    children: [
      _statPill('4', 'Roles'),
      _statPill('100%', 'Mobile'),
      _statPill('Real-time', 'Sync'),
    ],
  );

  Widget _statPill(String val, String label) => Column(
    children: [
      Text(
        val,
        style: const TextStyle(
          color: Colors.white,
          fontWeight: FontWeight.w900,
          fontSize: 22,
        ),
      ),
      Text(
        label,
        style: TextStyle(
          color: Colors.white.withValues(alpha: 0.7),
          fontSize: 12,
        ),
      ),
    ],
  );

  Widget _circle(double size, Color color) => Container(
    width: size,
    height: size,
    decoration: BoxDecoration(color: color, shape: BoxShape.circle),
  );

  Widget _field({
    required TextEditingController controller,
    required String label,
    required IconData icon,
    TextInputType? keyboard,
    bool obscure = false,
    Widget? suffix,
    VoidCallback? onSubmit,
  }) => TextField(
    controller: controller,
    obscureText: obscure,
    keyboardType: keyboard,
    onSubmitted: onSubmit != null ? (_) => onSubmit() : null,
    style: const TextStyle(color: AppColors.textPrimary, fontSize: 15),
    decoration: InputDecoration(
      labelText: label,
      labelStyle: const TextStyle(color: AppColors.textHint),
      prefixIcon: Icon(icon, color: AppColors.primary, size: 20),
      suffixIcon: suffix,
      filled: true,
      fillColor: Colors.white,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide.none,
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: AppColors.primary, width: 2),
      ),
      contentPadding: const EdgeInsets.symmetric(vertical: 16, horizontal: 14),
    ),
  );

  Widget _quickChip(String label, IconData icon, VoidCallback onTap) =>
      GestureDetector(
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: AppColors.primaryLight),
            boxShadow: [BoxShadow(color: AppColors.shadow, blurRadius: 4)],
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 14, color: AppColors.primary),
              const SizedBox(width: 5),
              Text(
                label,
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: AppColors.primaryDark,
                ),
              ),
            ],
          ),
        ),
      );
}
