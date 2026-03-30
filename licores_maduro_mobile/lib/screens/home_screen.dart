import 'package:flutter/material.dart';
import '../core/storage.dart';
import '../core/theme.dart';
import '../services/auth_service.dart';
import 'tracking/tracking_list_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _selectedIndex = 0;
  String _fullName = '';
  String _username = '';

  final _screens = const [
    TrackingListScreen(),
    // Future modules go here
    _PlaceholderScreen(label: 'Freight Forwarder', icon: Icons.anchor),
    _PlaceholderScreen(label: 'Cálculo de Costos', icon: Icons.calculate_outlined),
    _PlaceholderScreen(label: 'Stock', icon: Icons.inventory_outlined),
  ];

  @override
  void initState() {
    super.initState();
    _loadUser();
  }

  Future<void> _loadUser() async {
    final info = await AppStorage.getUserInfo();
    if (mounted) {
      setState(() {
        _fullName = info['fullName'] as String? ?? '';
        _username = info['username'] as String? ?? '';
      });
    }
  }

  Future<void> _logout() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Cerrar Sesión'),
        content: const Text('¿Deseas cerrar sesión?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Cancelar')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Salir')),
        ],
      ),
    );
    if (confirm == true && mounted) {
      await AuthService.logout();
      if (!mounted) return;
      Navigator.of(context).pushReplacementNamed('/');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      drawer: _buildDrawer(),
      body: _screens[_selectedIndex],
    );
  }

  Widget _buildDrawer() {
    return Drawer(
      child: Column(
        children: [
          // Header
          Container(
            width: double.infinity,
            color: AppColors.sidebarHeader,
            padding: const EdgeInsets.fromLTRB(20, 52, 20, 20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  width: 52,
                  height: 52,
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    shape: BoxShape.circle,
                    border: Border.all(color: Colors.white30, width: 2),
                  ),
                  child: const Icon(Icons.wine_bar_rounded,
                      color: Colors.white, size: 28),
                ),
                const SizedBox(height: 12),
                const Text(
                  'Licores Maduro',
                  style: TextStyle(
                      color: Colors.white,
                      fontSize: 17,
                      fontWeight: FontWeight.bold),
                ),
                if (_fullName.isNotEmpty)
                  Text(
                    _fullName,
                    style: TextStyle(
                        color: Colors.white.withOpacity(0.7), fontSize: 13),
                  ),
                if (_username.isNotEmpty)
                  Text(
                    '@$_username',
                    style: TextStyle(
                        color: Colors.white.withOpacity(0.5), fontSize: 12),
                  ),
              ],
            ),
          ),

          // Menu items
          Expanded(
            child: ListView(
              padding: const EdgeInsets.symmetric(vertical: 8),
              children: [
                _DrawerItem(
                  icon: Icons.local_shipping_outlined,
                  label: 'Tracking',
                  selected: _selectedIndex == 0,
                  onTap: () {
                    setState(() => _selectedIndex = 0);
                    Navigator.pop(context);
                  },
                ),
                _DrawerItem(
                  icon: Icons.anchor,
                  label: 'Freight Forwarder',
                  selected: _selectedIndex == 1,
                  onTap: () {
                    setState(() => _selectedIndex = 1);
                    Navigator.pop(context);
                  },
                ),
                _DrawerItem(
                  icon: Icons.calculate_outlined,
                  label: 'Cálculo de Costos',
                  selected: _selectedIndex == 2,
                  onTap: () {
                    setState(() => _selectedIndex = 2);
                    Navigator.pop(context);
                  },
                ),
                _DrawerItem(
                  icon: Icons.inventory_outlined,
                  label: 'Stock',
                  selected: _selectedIndex == 3,
                  onTap: () {
                    setState(() => _selectedIndex = 3);
                    Navigator.pop(context);
                  },
                ),
              ],
            ),
          ),

          // Logout
          const Divider(height: 1),
          ListTile(
            leading: const Icon(Icons.logout, color: AppColors.error),
            title: const Text('Cerrar Sesión',
                style: TextStyle(color: AppColors.error)),
            onTap: _logout,
          ),
          const SizedBox(height: 8),
        ],
      ),
    );
  }
}

class _DrawerItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;
  const _DrawerItem({
    required this.icon,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(icon,
          color: selected ? AppColors.primary : Colors.grey.shade600),
      title: Text(
        label,
        style: TextStyle(
          color: selected ? AppColors.primary : AppColors.textPrimary,
          fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
        ),
      ),
      selected: selected,
      selectedTileColor: AppColors.primary.withOpacity(0.08),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      contentPadding: const EdgeInsets.symmetric(horizontal: 16),
      onTap: onTap,
    );
  }
}

class _PlaceholderScreen extends StatelessWidget {
  final String label;
  final IconData icon;
  const _PlaceholderScreen({required this.label, required this.icon});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(label)),
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 64, color: Colors.grey.shade400),
            const SizedBox(height: 16),
            Text(
              label,
              style: TextStyle(fontSize: 18, color: Colors.grey.shade500),
            ),
            const SizedBox(height: 8),
            Text(
              'Próximamente',
              style: TextStyle(fontSize: 13, color: Colors.grey.shade400),
            ),
          ],
        ),
      ),
    );
  }
}
