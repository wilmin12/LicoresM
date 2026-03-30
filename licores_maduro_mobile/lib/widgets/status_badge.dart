import 'package:flutter/material.dart';
import '../core/theme.dart';

class StatusBadge extends StatelessWidget {
  final String code;
  final double fontSize;

  const StatusBadge({super.key, required this.code, this.fontSize = 11});

  static Color _colorForStatus(String code) {
    switch (code.toUpperCase()) {
      case 'INVIP':
        return const Color(0xFF607D8B);
      case 'INTRANSIT':
        return AppColors.info;
      case 'ATCUSTOMS':
        return AppColors.warning;
      case 'OVERDUE':
        return AppColors.error;
      case 'CLOSED':
        return AppColors.success;
      case 'DELIVERED':
        return const Color(0xFF00897B);
      default:
        return const Color(0xFF9E9E9E);
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _colorForStatus(code);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withOpacity(0.15),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: color.withOpacity(0.5)),
      ),
      child: Text(
        code,
        style: TextStyle(
          color: color,
          fontSize: fontSize,
          fontWeight: FontWeight.w600,
          letterSpacing: 0.3,
        ),
      ),
    );
  }
}

class WarehouseBadge extends StatelessWidget {
  final String? code;
  const WarehouseBadge({super.key, this.code});

  static String _label(String? code) {
    switch (code) {
      case '11010':
        return 'Duty Paid';
      case '11020':
        return 'Store';
      case '11060':
        return 'Duty Free';
      default:
        return code ?? '—';
    }
  }

  static Color _color(String? code) {
    switch (code) {
      case '11010':
        return AppColors.dutyPaid;
      case '11020':
        return AppColors.store;
      case '11060':
        return AppColors.dutyFree;
      default:
        return AppColors.textSecondary;
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _color(code);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
      decoration: BoxDecoration(
        color: color.withOpacity(0.12),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        _label(code),
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}
