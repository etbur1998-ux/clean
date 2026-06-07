import 'package:flutter/material.dart';

class AppColors {
  // ── Brand Colors (Teal/Emerald — professional cleaning theme) ──
  static const Color primary      = Color(0xFF00897B); // Teal 600
  static const Color primaryDark  = Color(0xFF00695C); // Teal 800
  static const Color primaryLight = Color(0xFFB2DFDB); // Teal 100
  static const Color secondary    = Color(0xFF26A69A); // Teal 400
  static const Color secondaryDark= Color(0xFF00796B); // Teal 700
  static const Color accent       = Color(0xFFF57F17); // Amber 800 — action accent

  // ── Surfaces ────────────────────────────────────────────────────
  static const Color background   = Color(0xFFF0F4F3);
  static const Color surface      = Color(0xFFFFFFFF);
  static const Color surfaceVar   = Color(0xFFE8F5E9);
  static const Color error        = Color(0xFFE53935);

  // ── Text ─────────────────────────────────────────────────────────
  static const Color textPrimary  = Color(0xFF1A2C2A);
  static const Color textSecondary= Color(0xFF4E6B68);
  static const Color textHint     = Color(0xFF90A4A2);

  // ── Misc ─────────────────────────────────────────────────────────
  static const Color divider      = Color(0xFFCFD8DC);
  static const Color shadow       = Color(0x18000000);

  // ── Gradients ────────────────────────────────────────────────────
  static const LinearGradient premiumGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF00897B), Color(0xFF26A69A)],
  );

  static const LinearGradient backgroundGradient = LinearGradient(
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
    colors: [Color(0xFFE0F2F1), Color(0xFFF0F4F3)],
  );

  static const LinearGradient cardGradient = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [Color(0xFF00897B), Color(0xFF00ACC1)],
  );

  // ── Status Colors ────────────────────────────────────────────────
  static const Color statusPending  = Color(0xFFFFF8E1);
  static const Color statusApproved = Color(0xFFE8F5E9);
  static const Color statusRejected = Color(0xFFFFEBEE);
  static const Color statusPaid     = Color(0xFFE3F2FD);
}
