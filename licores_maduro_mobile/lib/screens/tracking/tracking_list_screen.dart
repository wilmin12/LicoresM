import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../core/theme.dart';
import '../../models/tracking_order.dart';
import '../../services/tracking_service.dart';
import '../../widgets/kpi_card.dart';
import '../../widgets/status_badge.dart';

class TrackingListScreen extends StatefulWidget {
  const TrackingListScreen({super.key});

  @override
  State<TrackingListScreen> createState() => _TrackingListScreenState();
}

class _TrackingListScreenState extends State<TrackingListScreen> {
  final _searchCtrl = TextEditingController();
  final _scrollCtrl = ScrollController();

  List<TrackingOrderDto> _orders = [];
  List<OrderStatus> _statuses = [];
  bool _loading = false;
  bool _loadingMore = false;
  String? _error;
  int _page = 1;
  int _totalCount = 0;
  bool _hasMore = true;

  String? _filterStatus;
  String? _filterWarehouse;
  String? _selectedKpi; // null = all

  // KPI counts
  int _total = 0;
  int _inVip = 0;
  int _inTransit = 0;
  int _atCustoms = 0;
  int _overdue = 0;
  int _closed = 0;

  static const _pageSize = 20;

  @override
  void initState() {
    super.initState();
    _loadStatuses();
    _loadOrders(reset: true);
    _scrollCtrl.addListener(_onScroll);
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _scrollCtrl.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollCtrl.position.pixels >=
            _scrollCtrl.position.maxScrollExtent - 200 &&
        !_loadingMore &&
        _hasMore) {
      _loadMore();
    }
  }

  Future<void> _loadStatuses() async {
    try {
      final statuses = await TrackingService.getOrderStatuses();
      if (mounted) setState(() => _statuses = statuses);
    } catch (_) {}
  }

  Future<void> _loadOrders({bool reset = false}) async {
    if (_loading) return;
    setState(() {
      _loading = true;
      _error = null;
      if (reset) {
        _orders = [];
        _page = 1;
        _hasMore = true;
      }
    });
    try {
      final result = await TrackingService.getOrders(
        search: _searchCtrl.text.trim(),
        status: _filterStatus,
        warehouse: _filterWarehouse,
        page: 1,
        pageSize: 200, // load more for KPI counts
      );
      if (mounted) {
        setState(() {
          _orders = result.items;
          _totalCount = result.totalCount;
          _page = 1;
          _hasMore = result.items.length < result.totalCount;
          _computeKpis(result.items);
        });
      }
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _loadMore() async {
    if (_loadingMore || !_hasMore) return;
    setState(() => _loadingMore = true);
    try {
      final next = _page + 1;
      final result = await TrackingService.getOrders(
        search: _searchCtrl.text.trim(),
        status: _filterStatus,
        warehouse: _filterWarehouse,
        page: next,
        pageSize: _pageSize,
      );
      if (mounted) {
        setState(() {
          _orders.addAll(result.items);
          _page = next;
          _hasMore = _orders.length < result.totalCount;
        });
      }
    } catch (_) {} finally {
      if (mounted) setState(() => _loadingMore = false);
    }
  }

  void _computeKpis(List<TrackingOrderDto> all) {
    _total = all.length;
    _inVip = all.where((o) => o.order.trStatusCode == 'INVIP').length;
    _inTransit = all.where((o) => o.order.trStatusCode == 'INTRANSIT').length;
    _atCustoms = all.where((o) => o.order.trStatusCode == 'ATCUSTOMS').length;
    _overdue = all.where((o) => o.order.trStatusCode == 'OVERDUE').length;
    _closed = all
        .where((o) =>
            o.order.trStatusCode == 'CLOSED' ||
            o.order.trStatusCode == 'DELIVERED')
        .length;
  }

  List<TrackingOrderDto> get _filtered {
    if (_selectedKpi == null) return _orders;
    return _orders.where((o) {
      switch (_selectedKpi) {
        case 'INVIP':
          return o.order.trStatusCode == 'INVIP';
        case 'INTRANSIT':
          return o.order.trStatusCode == 'INTRANSIT';
        case 'ATCUSTOMS':
          return o.order.trStatusCode == 'ATCUSTOMS';
        case 'OVERDUE':
          return o.order.trStatusCode == 'OVERDUE';
        case 'CLOSED':
          return o.order.trStatusCode == 'CLOSED' ||
              o.order.trStatusCode == 'DELIVERED';
        default:
          return true;
      }
    }).toList();
  }

  Future<void> _autoImport() async {
    try {
      final imported = await TrackingService.autoImportVip();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(imported > 0
              ? '$imported orden(es) importada(s) desde VIP'
              : 'No hay órdenes nuevas en VIP'),
          backgroundColor:
              imported > 0 ? AppColors.success : AppColors.textSecondary,
        ));
        if (imported > 0) _loadOrders(reset: true);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text('Error: $e'),
          backgroundColor: AppColors.error,
        ));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Tracking de Órdenes'),
        actions: [
          IconButton(
            icon: const Icon(Icons.sync),
            tooltip: 'Auto-import VIP',
            onPressed: _autoImport,
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => _loadOrders(reset: true),
          ),
        ],
      ),
      body: Column(
        children: [
          // Search + filters
          _buildSearchBar(),

          // KPI cards
          _buildKpiRow(),

          // Results count
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
            child: Row(
              children: [
                Text(
                  '${_filtered.length} de $_totalCount órdenes',
                  style: const TextStyle(
                      fontSize: 12, color: AppColors.textSecondary),
                ),
              ],
            ),
          ),

          // List
          Expanded(child: _buildList()),
        ],
      ),
    );
  }

  Widget _buildSearchBar() {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 8),
      child: Column(
        children: [
          TextField(
            controller: _searchCtrl,
            decoration: InputDecoration(
              hintText: 'Buscar PO, proveedor, contenedor...',
              hintStyle: const TextStyle(fontSize: 13),
              prefixIcon: const Icon(Icons.search, size: 18),
              suffixIcon: _searchCtrl.text.isNotEmpty
                  ? IconButton(
                      icon: const Icon(Icons.clear, size: 16),
                      onPressed: () {
                        _searchCtrl.clear();
                        _loadOrders(reset: true);
                      },
                    )
                  : null,
              isDense: true,
              contentPadding: const EdgeInsets.symmetric(vertical: 8),
            ),
            style: const TextStyle(fontSize: 13),
            onSubmitted: (_) => _loadOrders(reset: true),
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              Expanded(child: _statusDropdown()),
              const SizedBox(width: 6),
              Expanded(child: _warehouseDropdown()),
            ],
          ),
        ],
      ),
    );
  }

  Widget _statusDropdown() {
    return DropdownButtonFormField<String>(
      value: _filterStatus,
      isExpanded: true,
      decoration: const InputDecoration(
        labelText: 'Estado',
        labelStyle: TextStyle(fontSize: 12),
        isDense: true,
        contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      ),
      style: const TextStyle(fontSize: 12, color: Colors.black87),
      items: [
        const DropdownMenuItem(value: null, child: Text('Todos')),
        ..._statuses.map((s) => DropdownMenuItem(
              value: s.osCode,
              child: Text(s.osDescription, overflow: TextOverflow.ellipsis),
            )),
      ],
      onChanged: (v) {
        setState(() => _filterStatus = v);
        _loadOrders(reset: true);
      },
    );
  }

  Widget _warehouseDropdown() {
    return DropdownButtonFormField<String>(
      value: _filterWarehouse,
      isExpanded: true,
      decoration: const InputDecoration(
        labelText: 'Almacén',
        labelStyle: TextStyle(fontSize: 12),
        isDense: true,
        contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      ),
      style: const TextStyle(fontSize: 12, color: Colors.black87),
      items: const [
        DropdownMenuItem(value: null, child: Text('Todos')),
        DropdownMenuItem(value: '11010', child: Text('Duty Paid')),
        DropdownMenuItem(value: '11020', child: Text('Store')),
        DropdownMenuItem(value: '11060', child: Text('Duty Free')),
      ],
      onChanged: (v) {
        setState(() => _filterWarehouse = v);
        _loadOrders(reset: true);
      },
    );
  }

  Widget _buildKpiRow() {
    return Container(
      height: 90,                           // reducido de 110 → 90
      color: Colors.white,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        children: [
          KpiCard(
            label: 'Total',
            value: '$_total',
            color: AppColors.primary,
            icon: Icons.local_shipping_outlined,
            isSelected: _selectedKpi == null,
            onTap: () => setState(() => _selectedKpi = null),
          ),
          const SizedBox(width: 8),
          KpiCard(
            label: 'In VIP',
            value: '$_inVip',
            color: const Color(0xFF607D8B),
            icon: Icons.inventory_2_outlined,
            isSelected: _selectedKpi == 'INVIP',
            onTap: () =>
                setState(() => _selectedKpi = _selectedKpi == 'INVIP' ? null : 'INVIP'),
          ),
          const SizedBox(width: 8),
          KpiCard(
            label: 'En Tránsito',
            value: '$_inTransit',
            color: AppColors.info,
            icon: Icons.directions_boat_outlined,
            isSelected: _selectedKpi == 'INTRANSIT',
            onTap: () => setState(() =>
                _selectedKpi = _selectedKpi == 'INTRANSIT' ? null : 'INTRANSIT'),
          ),
          const SizedBox(width: 8),
          KpiCard(
            label: 'En Aduana',
            value: '$_atCustoms',
            color: AppColors.warning,
            icon: Icons.account_balance_outlined,
            isSelected: _selectedKpi == 'ATCUSTOMS',
            onTap: () => setState(() =>
                _selectedKpi = _selectedKpi == 'ATCUSTOMS' ? null : 'ATCUSTOMS'),
          ),
          const SizedBox(width: 8),
          KpiCard(
            label: 'Vencido',
            value: '$_overdue',
            color: AppColors.error,
            icon: Icons.warning_amber_outlined,
            isSelected: _selectedKpi == 'OVERDUE',
            onTap: () => setState(
                () => _selectedKpi = _selectedKpi == 'OVERDUE' ? null : 'OVERDUE'),
          ),
          const SizedBox(width: 8),
          KpiCard(
            label: 'Cerrado',
            value: '$_closed',
            color: AppColors.success,
            icon: Icons.check_circle_outline,
            isSelected: _selectedKpi == 'CLOSED',
            onTap: () => setState(
                () => _selectedKpi = _selectedKpi == 'CLOSED' ? null : 'CLOSED'),
          ),
        ],
      ),
    );
  }

  Widget _buildList() {
    if (_loading && _orders.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.error_outline, color: AppColors.error, size: 48),
              const SizedBox(height: 12),
              Text(_error!, textAlign: TextAlign.center,
                  style: const TextStyle(color: AppColors.error)),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: () => _loadOrders(reset: true),
                child: const Text('Reintentar'),
              ),
            ],
          ),
        ),
      );
    }
    final items = _filtered;
    if (items.isEmpty) {
      return const Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.inbox_outlined, size: 56, color: Colors.grey),
            SizedBox(height: 12),
            Text('No se encontraron órdenes',
                style: TextStyle(color: Colors.grey)),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => _loadOrders(reset: true),
      child: ListView.builder(
        controller: _scrollCtrl,
        itemCount: items.length + (_loadingMore ? 1 : 0),
        itemBuilder: (ctx, i) {
          if (i == items.length) {
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          return _OrderCard(dto: items[i]);
        },
      ),
    );
  }
}

class _OrderCard extends StatelessWidget {
  final TrackingOrderDto dto;
  const _OrderCard({required this.dto});

  @override
  Widget build(BuildContext context) {
    final o = dto.order;
    final dateFormat = DateFormat('dd/MM/yyyy');
    final arrivalDate = o.trEstArrivalDate != null
        ? dateFormat.format(o.trEstArrivalDate!)
        : fmtVipDate(o.trVipArrivalDate);

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () {
          Navigator.of(context).pushNamed(
            '/tracking/detail',
            arguments: o.trId,
          );
        },
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header row
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'PO: ${o.trPoNo}',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 15,
                      color: AppColors.primary,
                    ),
                  ),
                  if (o.trStatusCode != null)
                    StatusBadge(code: o.trStatusCode!),
                ],
              ),
              const SizedBox(height: 6),

              // Supplier
              if (o.trSupplierName != null || o.trSupplier != null)
                Text(
                  o.trSupplierName ?? o.trSupplier ?? '',
                  style: const TextStyle(
                      fontSize: 13, color: AppColors.textSecondary),
                  overflow: TextOverflow.ellipsis,
                ),
              const SizedBox(height: 8),

              // Info row
              Row(
                children: [
                  if (o.trWarehouse != null) ...[
                    WarehouseBadge(code: o.trWarehouse),
                    const SizedBox(width: 8),
                  ],
                  if (o.trBorw != null)
                    _chip(
                      o.trBorw == 'B' ? 'Beer' : 'Wine',
                      o.trBorw == 'B'
                          ? const Color(0xFFE65100)
                          : const Color(0xFF6A1B9A),
                    ),
                ],
              ),
              const SizedBox(height: 8),
              const Divider(height: 1),
              const SizedBox(height: 8),

              // Bottom row: container + arrival + cases
              Row(
                children: [
                  _infoItem(
                    Icons.view_in_ar_outlined,
                    o.trContainerNumber ?? '—',
                  ),
                  const SizedBox(width: 16),
                  _infoItem(
                    Icons.calendar_today_outlined,
                    arrivalDate,
                  ),
                  const Spacer(),
                  if (o.trTotalCases != null)
                    Text(
                      '${o.trTotalCases!.toStringAsFixed(0)} cs',
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textSecondary,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                ],
              ),

              // Days over container
              if ((dto.daysOverContainer ?? 0) > 0) ...[
                const SizedBox(height: 6),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: AppColors.error.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    '${dto.daysOverContainer} días sobre contenedor',
                    style: const TextStyle(
                        color: AppColors.error,
                        fontSize: 11,
                        fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _chip(String label, Color color) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
        decoration: BoxDecoration(
          color: color.withOpacity(0.12),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Text(label,
            style: TextStyle(
                color: color, fontSize: 11, fontWeight: FontWeight.w600)),
      );

  Widget _infoItem(IconData icon, String label) => Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 13, color: AppColors.textSecondary),
          const SizedBox(width: 4),
          Text(label,
              style: const TextStyle(
                  fontSize: 12, color: AppColors.textSecondary)),
        ],
      );
}
