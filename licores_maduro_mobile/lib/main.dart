import 'package:flutter/material.dart';
import 'core/theme.dart';
import 'core/storage.dart';
import 'screens/login_screen.dart';
import 'screens/home_screen.dart';
import 'screens/tracking/tracking_detail_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const LicoresMaduoApp());
}

class LicoresMaduoApp extends StatelessWidget {
  const LicoresMaduoApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Licores Maduro',
      theme: AppTheme.light,
      debugShowCheckedModeBanner: false,
      home: const _SplashGate(),
      onGenerateRoute: _onGenerateRoute,
    );
  }

  Route<dynamic>? _onGenerateRoute(RouteSettings settings) {
    switch (settings.name) {
      case '/':
        return MaterialPageRoute(builder: (_) => const _SplashGate());
      case '/home':
        return MaterialPageRoute(builder: (_) => const HomeScreen());
      case '/tracking/detail':
        final id = settings.arguments as int;
        return MaterialPageRoute(
          builder: (_) => TrackingDetailScreen(orderId: id),
        );
      default:
        return MaterialPageRoute(
          builder: (_) => const Scaffold(
            body: Center(child: Text('Página no encontrada')),
          ),
        );
    }
  }
}

/// Checks if token exists and routes to login or home
class _SplashGate extends StatefulWidget {
  const _SplashGate();

  @override
  State<_SplashGate> createState() => _SplashGateState();
}

class _SplashGateState extends State<_SplashGate> {
  @override
  void initState() {
    super.initState();
    _check();
  }

  Future<void> _check() async {
    await Future.delayed(const Duration(milliseconds: 500));
    final token = await AppStorage.getToken();
    if (!mounted) return;
    if (token != null && token.isNotEmpty) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const HomeScreen()),
      );
    } else {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const LoginScreen()),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: AppColors.sidebar,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.wine_bar_rounded, size: 64, color: Colors.white),
            SizedBox(height: 16),
            Text(
              'Licores Maduro',
              style: TextStyle(
                color: Colors.white,
                fontSize: 24,
                fontWeight: FontWeight.bold,
              ),
            ),
            SizedBox(height: 32),
            CircularProgressIndicator(color: Colors.white54),
          ],
        ),
      ),
    );
  }
}
