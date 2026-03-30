import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../core/theme.dart';
import '../../models/tracking_order.dart';
import '../../services/tracking_service.dart';
import '../../widgets/status_badge.dart';

class TrackingDetailScreen extends StatefulWidget {
  final int orderId;
  const TrackingDetailScreen({super.key, required this.orderId});

  @override
  State<TrackingDetailScreen> createState() => _TrackingDetailScreenState();
}

class _TrackingDetailScreenState extends State<TrackingDetailScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabCtrl;
  TrackingOrderDto? _dto;
  bool _loading = true;
  String? _error;
  bool _syncing = false;

  static const _tabs = [
    'General',
    'Embarque',
    'Documentos',
    'Aduana',
    'Descarga',
    'Historial',
  ];

  @override
  void initState() {
    super.initState();
    _tabCtrl = TabController(length: _tabs.length, vsync: this);
    _loadOrder();
  }

  @override
  void dispose() {
    _tabCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadOrder() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final dto = await TrackingService.getOrder(widget.orderId);
      if (mounted) setState(() => _dto = dto);
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _syncVip() async {
    setState(() => _syncing = true);
    try {
      final dto = await TrackingService.syncVip(widget.orderId);
      if (mounted) {
        setState(() => _dto = dto);
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
          content: Text('Datos VIP sincronizados'),
          backgroundColor: AppColors.success,
        ));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text('Error: $e'),
          backgroundColor: AppColors.error,
        ));
      }
    } finally {
      if (mounted) setState(() => _syncing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          _dto != null ? 'PO: ${_dto!.order.trPoNo}' : 'Detalle de Orden',
        ),
        actions: [
          if (_syncing)
            const Padding(
              padding: EdgeInsets.all(14),
              child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(
                      strokeWidth: 2, color: Colors.white)),
            )
          else
            IconButton(
              icon: const Icon(Icons.sync),
              tooltip: 'Sync VIP',
              onPressed: _syncVip,
            ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadOrder,
          ),
        ],
        bottom: TabBar(
          controller: _tabCtrl,
          isScrollable: true,
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white60,
          indicatorColor: Colors.white,
          indicatorWeight: 3,
          tabs: _tabs.map((t) => Tab(text: t)).toList(),
        ),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _buildError()
              : _buildTabs(),
    );
  }

  Widget _buildError() => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.error_outline, color: AppColors.error, size: 48),
              const SizedBox(height: 12),
              Text(_error!, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              ElevatedButton(onPressed: _loadOrder, child: const Text('Reintentar')),
            ],
          ),
        ),
      );

  Widget _buildTabs() {
    final o = _dto!.order;
    return TabBarView(
      controller: _tabCtrl,
      children: [
        _TabGeneral(order: o, dto: _dto!),
        _TabEmbarque(order: o),
        _TabDocumentos(order: o),
        _TabAduana(order: o),
        _TabDescarga(order: o, dto: _dto!),
        _TabHistorial(history: _dto!.statusHistory),
      ],
    );
  }
}

// ── Tab: General ─────────────────────────────────────────────────────────────

class _TabGeneral extends StatelessWidget {
  final TrackingOrder order;
  final TrackingOrderDto dto;
  const _TabGeneral({required this.order, required this.dto});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          // Status card
          _InfoCard(title: 'Estado y Clasificación', children: [
            _Row('Estado', order.trStatusCode != null
                ? StatusBadge(code: order.trStatusCode!)
                : const Text('—')),
            _Field('PO #', order.trPoNo),
            _Field('B/W', order.trBorw == 'B' ? 'Beer' : order.trBorw == 'W' ? 'Wine' : order.trBorw),
            _Field('Almacén', _warehouseLabel(order.trWarehouse)),
            _Field('País', order.trCountry),
            _Field('Freight Forwarder', order.trFreightForwarder),
          ]),

          // VIP data
          _InfoCard(title: 'Datos VIP', children: [
            _Field('Proveedor', order.trSupplierName ?? order.trSupplier),
            _Field('Código Proveedor', order.trSupplierCode),
            _Field('Estado VIP', order.trVipStatus),
            _Field('Fecha Orden', fmtVipDate(order.trOrderDate)),
            _Field('Fecha Embarque VIP', fmtVipDate(order.trVipShipDate)),
            _Field('Fecha Llegada VIP', fmtVipDate(order.trVipArrivalDate)),
            _Field('Total Cajas', order.trTotalCases?.toStringAsFixed(0)),
            _Field('Peso (kg)', order.trVipWeight?.toStringAsFixed(2)),
            _Field('Litros', order.trVipLiters?.toStringAsFixed(2)),
            _Field('Monto Total', order.trVipTotalAmount != null
                ? '\$${order.trVipTotalAmount!.toStringAsFixed(2)}'
                : null),
            _Field('Líneas', order.trVipTotalLines?.toString()),
          ]),

          if (order.trComments != null)
            _InfoCard(title: 'Comentarios', children: [
              _TextBlock(order.trComments!),
            ]),

          if (dto.daysOverContainer != null && dto.daysOverContainer! > 0)
            Container(
              margin: const EdgeInsets.only(top: 8),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.error.withOpacity(0.1),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppColors.error.withOpacity(0.3)),
              ),
              child: Row(
                children: [
                  const Icon(Icons.warning_amber, color: AppColors.error),
                  const SizedBox(width: 12),
                  Text(
                    '${dto.daysOverContainer} días sobre contenedor',
                    style: const TextStyle(
                        color: AppColors.error, fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }

  String _warehouseLabel(String? code) {
    switch (code) {
      case '11010':
        return 'Duty Paid (11010)';
      case '11020':
        return 'Store (11020)';
      case '11060':
        return 'Duty Free (11060)';
      default:
        return code ?? '—';
    }
  }
}

// ── Tab: Embarque ─────────────────────────────────────────────────────────────

class _TabEmbarque extends StatelessWidget {
  final TrackingOrder order;
  const _TabEmbarque({required this.order});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          _InfoCard(title: 'Contenedor', children: [
            _Field('Número Contenedor', order.trContainerNumber),
            _Field('Tamaño Contenedor', order.trContainerSize),
            _Field('Referencia Consolidación', order.trConsolidationRef),
          ]),
          _InfoCard(title: 'Línea Naviera', children: [
            _Field('Línea Naviera', order.trShippingLine),
            _Field('Agente', order.trShippingAgent),
            _Field('Barco/Vessel', order.trVessel),
            _Field('Tiempo de Tránsito', order.trTransitTime),
          ]),
          _InfoCard(title: 'Fechas de Embarque', children: [
            _Field('Acknowled Order', _boolLabel(order.trAcknowledgeOrder)),
            _Field('Fecha Carga Shipper', _fmtDate(order.trDateLoadingShipper)),
            _Field('Pro Forma Recibida', _fmtDate(order.trDateProFormaReceived)),
            _Field('Cant. Pro Forma', order.trQtyProForma?.toStringAsFixed(0)),
            _Field('Listo en Fábrica', _fmtDate(order.trFactoryReadyDate)),
            _Field('Est. Salida', _fmtDate(order.trEstDepartureDate)),
            _Field('Est. Llegada', _fmtDate(order.trEstArrivalDate)),
            _Field('ETA Solicitada', _fmtDate(order.trRequestedEta)),
          ]),
        ],
      ),
    );
  }
}

// ── Tab: Documentos ───────────────────────────────────────────────────────────

class _TabDocumentos extends StatelessWidget {
  final TrackingOrder order;
  const _TabDocumentos({required this.order});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          _InfoCard(title: 'Factura', children: [
            _Field('N° Factura', order.trInvoiceNumber),
            _Field('Fecha Llegada Factura', _fmtDate(order.trDateArrivalInvoice)),
          ]),
          _InfoCard(title: 'Bill of Lading', children: [
            _Field('Bijlage Done', _boolLabel(order.trBijlageDone)),
            _Field('Fecha Llegada B/L', _fmtDate(order.trDateArrivalBol)),
          ]),
          _InfoCard(title: 'Otros Documentos', children: [
            _Field('Fecha Nota de Llegada', _fmtDate(order.trDateArrivalNoteReceived)),
            _Field('Fecha Manifiesto', _fmtDate(order.trDateManifestReceived)),
            _Field('Copias al Declarante', _fmtDate(order.trDateCopiesToDeclarant)),
          ]),
          if (order.trRemarks != null)
            _InfoCard(title: 'Observaciones', children: [
              _TextBlock(order.trRemarks!),
            ]),
        ],
      ),
    );
  }
}

// ── Tab: Aduana ────────────────────────────────────────────────────────────────

class _TabAduana extends StatelessWidget {
  final TrackingOrder order;
  const _TabAduana({required this.order});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          _InfoCard(title: 'Trámite Aduanero', children: [
            _Field('Papeles Listos', _fmtDate(order.trDateCustomsPapersReady)),
            _Field('Asycuda', _fmtDate(order.trDateCustomsPapersAsycuda)),
            _Field('N° SAD', order.trSadNumber),
            _Field('N° BC Órdenes', order.trBcNumberOrders),
          ]),
          _InfoCard(title: 'CPS', children: [
            _Field('Contenedor en CPS', _fmtDate(order.trDateContainerAtCps)),
            _Field('Vencimiento CPS', _fmtDate(order.trExpirationDateCps)),
            _Field('Papeles a CPS', _fmtDate(order.trDateCustomsPapersToCps)),
          ]),
          _InfoCard(title: 'Llegada', children: [
            _Field('Llegada a Licores', _fmtDate(order.trDateContainerArrivedLicores)),
            _Field('Abierto en Aduana', _fmtDate(order.trDateContainerOpenedCustoms)),
          ]),
          if (order.trIssuesComments != null)
            _InfoCard(title: 'Problemas / Comentarios', children: [
              _TextBlock(order.trIssuesComments!),
            ]),
        ],
      ),
    );
  }
}

// ── Tab: Descarga ─────────────────────────────────────────────────────────────

class _TabDescarga extends StatelessWidget {
  final TrackingOrder order;
  final TrackingOrderDto dto;
  const _TabDescarga({required this.order, required this.dto});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          _InfoCard(title: 'Descarga y Devolución', children: [
            _Field('Listo para Descarga', _fmtDate(order.trDateContainerUnloadReady)),
            _Field('Devolución Contenedor', _fmtDate(order.trReturnDateContainer)),
            _Field('Papeles Admin Descarga', _fmtDate(order.trDateUnloadPapersAdmin)),
            _Field('N° Nota de Salida', order.trExitNoteNumber),
          ]),
          if (dto.daysOverContainer != null)
            _InfoCard(title: 'Días sobre Contenedor', children: [
              _Row('Días',
                  Text(
                    '${dto.daysOverContainer ?? 0}',
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: (dto.daysOverContainer ?? 0) > 0
                          ? AppColors.error
                          : AppColors.success,
                    ),
                  )),
            ]),
          _InfoCard(title: 'Auditoría', children: [
            _Field('Creado por', order.trCreatedBy),
            _Field('Creado el', _fmtDate(order.trCreatedAt)),
            _Field('Actualizado por', order.trUpdatedBy),
            _Field('Actualizado el', _fmtDate(order.trUpdatedAt)),
          ]),
        ],
      ),
    );
  }
}

// ── Tab: Historial ────────────────────────────────────────────────────────────

class _TabHistorial extends StatelessWidget {
  final List<TrackingStatusHistory> history;
  const _TabHistorial({required this.history});

  @override
  Widget build(BuildContext context) {
    if (history.isEmpty) {
      return const Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.history, size: 48, color: Colors.grey),
            SizedBox(height: 8),
            Text('Sin historial', style: TextStyle(color: Colors.grey)),
          ],
        ),
      );
    }
    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: history.length,
      itemBuilder: (ctx, i) {
        final h = history[i];
        return Card(
          margin: const EdgeInsets.only(bottom: 8),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: AppColors.primary.withOpacity(0.1),
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Icons.history,
                      size: 18, color: AppColors.primary),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          StatusBadge(code: h.tshStatusCode),
                          if (h.tshStatusDate != null)
                            Text(
                              DateFormat('dd/MM/yyyy HH:mm')
                                  .format(h.tshStatusDate!.toLocal()),
                              style: const TextStyle(
                                  fontSize: 11, color: AppColors.textSecondary),
                            ),
                        ],
                      ),
                      if (h.tshComments != null && h.tshComments!.isNotEmpty)
                        Padding(
                          padding: const EdgeInsets.only(top: 6),
                          child: Text(h.tshComments!,
                              style: const TextStyle(fontSize: 13)),
                        ),
                      if (h.tshChangedBy != null)
                        Padding(
                          padding: const EdgeInsets.only(top: 4),
                          child: Text(
                            'Por: ${h.tshChangedBy}',
                            style: const TextStyle(
                                fontSize: 11, color: AppColors.textSecondary),
                          ),
                        ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

// ── Shared widgets ─────────────────────────────────────────────────────────────

class _InfoCard extends StatelessWidget {
  final String title;
  final List<Widget> children;
  const _InfoCard({required this.title, required this.children});

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: const TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 14,
                color: AppColors.primary,
                letterSpacing: 0.3,
              ),
            ),
            const Divider(height: 16),
            ...children,
          ],
        ),
      ),
    );
  }
}

class _Field extends StatelessWidget {
  final String label;
  final String? value;
  const _Field(this.label, this.value);

  @override
  Widget build(BuildContext context) {
    if (value == null || value!.isEmpty) return const SizedBox.shrink();
    // Usa el 38% del ancho disponible para el label (responsive)
    final labelWidth = MediaQuery.of(context).size.width * 0.38;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: labelWidth,
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 12,
                color: AppColors.textSecondary,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value!,
              style: const TextStyle(fontSize: 12, color: AppColors.textPrimary),
            ),
          ),
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  final String label;
  final Widget child;
  const _Row(this.label, this.child);

  @override
  Widget build(BuildContext context) {
    final labelWidth = MediaQuery.of(context).size.width * 0.38;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          SizedBox(
            width: labelWidth,
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 12,
                color: AppColors.textSecondary,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
          child,
        ],
      ),
    );
  }
}

class _TextBlock extends StatelessWidget {
  final String text;
  const _TextBlock(this.text);

  @override
  Widget build(BuildContext context) {
    return Text(text,
        style: const TextStyle(fontSize: 13, color: AppColors.textPrimary));
  }
}

// ── Helpers ────────────────────────────────────────────────────────────────────

String _fmtDate(DateTime? dt) {
  if (dt == null) return '—';
  return DateFormat('dd/MM/yyyy').format(dt.toLocal());
}

String _boolLabel(bool? v) {
  if (v == null) return '—';
  return v ? 'Sí' : 'No';
}
