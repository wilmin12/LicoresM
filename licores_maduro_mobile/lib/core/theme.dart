import 'package:flutter/material.dart';

class AppColors {
  static const primary = Color(0xFFA52535);
  static const primaryDark = Color(0xFF7B1A2E);
  static const sidebar = Color(0xFF3D1525);
  static const sidebarHeader = Color(0xFF2C0F1A);
  static const background = Color(0xFFF4F6F9);
  static const surface = Colors.white;
  static const error = Color(0xFFD32F2F);
  static const success = Color(0xFF2E7D32);
  static const warning = Color(0xFFF57C00);
  static const info = Color(0xFF0288D1);
  static const textPrimary = Color(0xFF1A1A2E);
  static const textSecondary = Color(0xFF6B7280);

  // Warehouse badge colors
  static const dutyPaid = Color(0xFF1565C0);
  static const store = Color(0xFF2E7D32);
  static const dutyFree = Color(0xFF6A1B9A);
}

class AppTheme {
  static ThemeData get light => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: AppColors.primary,
          primary: AppColors.primary,
          onPrimary: Colors.white,
          secondary: AppColors.primaryDark,
          surface: AppColors.surface,
          surfaceContainerHighest: AppColors.background,
          error: AppColors.error,
        ),
        scaffoldBackgroundColor: AppColors.background,
        appBarTheme: const AppBarTheme(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          elevation: 0,
          centerTitle: false,
          titleTextStyle: TextStyle(
            color: Colors.white,
            fontSize: 18,
            fontWeight: FontWeight.w600,
            letterSpacing: 0.3,
          ),
        ),
        cardTheme: CardThemeData(
          elevation: 1,
          color: AppColors.surface,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          margin: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: Colors.white,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: Color(0xFFE0E0E0)),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: Color(0xFFE0E0E0)),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: AppColors.primary, width: 2),
          ),
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        ),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            foregroundColor: Colors.white,
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
          ),
        ),
        chipTheme: ChipThemeData(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        ),
        dividerTheme: const DividerThemeData(
          color: Color(0xFFEEEEEE),
          thickness: 1,
        ),
      );
}
