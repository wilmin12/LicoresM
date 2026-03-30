// API usa PropertyNamingPolicy = null → devuelve PascalCase

DateTime? _dt(dynamic v) => v != null ? DateTime.tryParse(v.toString()) : null;

class TrackingStatusHistory {
  final int tshId;
  final int tshTrackingId;
  final String tshPoNo;
  final String tshStatusCode;
  final DateTime? tshStatusDate;
  final String? tshComments;
  final String? tshChangedBy;

  TrackingStatusHistory({
    required this.tshId,
    required this.tshTrackingId,
    required this.tshPoNo,
    required this.tshStatusCode,
    this.tshStatusDate,
    this.tshComments,
    this.tshChangedBy,
  });

  factory TrackingStatusHistory.fromJson(Map<String, dynamic> j) =>
      TrackingStatusHistory(
        tshId: j['TshId'] as int? ?? 0,
        tshTrackingId: j['TshTrackingId'] as int? ?? 0,
        tshPoNo: j['TshPoNo'] as String? ?? '',
        tshStatusCode: j['TshStatusCode'] as String? ?? '',
        tshStatusDate: _dt(j['TshStatusDate']),
        tshComments: j['TshComments'] as String?,
        tshChangedBy: j['TshChangedBy'] as String?,
      );
}

class TrackingOrder {
  final int trId;
  final String trPoNo;
  final String? trWarehouse;
  final String? trSupplier;
  final String? trSupplierCode;
  final String? trSupplierName;
  final String? trBorw;
  final int? trOrderDate;
  final int? trVipShipDate;
  final int? trVipArrivalDate;
  final String? trStatusCode;
  final String? trFreightForwarder;
  final String? trContainerNumber;
  final String? trContainerSize;
  final String? trShippingLine;
  final String? trShippingAgent;
  final String? trVessel;
  final String? trConsolidationRef;
  final String? trTransitTime;
  final String? trInvoiceNumber;
  final String? trSadNumber;
  final String? trBcNumberOrders;
  final String? trExitNoteNumber;
  final String? trComments;
  final String? trRemarks;
  final String? trIssuesComments;
  final String? trCountry;
  final String? trVipStatus;
  final double? trTotalCases;
  final double? trVipWeight;
  final double? trVipLiters;
  final double? trVipTotalAmount;
  final int? trVipTotalLines;
  final double? trQtyProForma;
  final bool? trAcknowledgeOrder;
  final bool? trBijlageDone;
  final DateTime? trLastUpdateDate;
  final DateTime? trRequestedEta;
  final DateTime? trDateLoadingShipper;
  final DateTime? trDateProFormaReceived;
  final DateTime? trFactoryReadyDate;
  final DateTime? trEstDepartureDate;
  final DateTime? trEstArrivalDate;
  final DateTime? trDateArrivalInvoice;
  final DateTime? trDateArrivalBol;
  final DateTime? trDateArrivalNoteReceived;
  final DateTime? trDateManifestReceived;
  final DateTime? trDateCopiesToDeclarant;
  final DateTime? trDateCustomsPapersReady;
  final DateTime? trDateCustomsPapersAsycuda;
  final DateTime? trDateContainerAtCps;
  final DateTime? trExpirationDateCps;
  final DateTime? trDateCustomsPapersToCps;
  final DateTime? trDateContainerArrivedLicores;
  final DateTime? trDateContainerOpenedCustoms;
  final DateTime? trDateContainerUnloadReady;
  final DateTime? trReturnDateContainer;
  final DateTime? trDateUnloadPapersAdmin;
  final String? trCreatedBy;
  final DateTime? trCreatedAt;
  final String? trUpdatedBy;
  final DateTime? trUpdatedAt;

  TrackingOrder({
    required this.trId,
    required this.trPoNo,
    this.trWarehouse,
    this.trSupplier,
    this.trSupplierCode,
    this.trSupplierName,
    this.trBorw,
    this.trOrderDate,
    this.trVipShipDate,
    this.trVipArrivalDate,
    this.trStatusCode,
    this.trFreightForwarder,
    this.trContainerNumber,
    this.trContainerSize,
    this.trShippingLine,
    this.trShippingAgent,
    this.trVessel,
    this.trConsolidationRef,
    this.trTransitTime,
    this.trInvoiceNumber,
    this.trSadNumber,
    this.trBcNumberOrders,
    this.trExitNoteNumber,
    this.trComments,
    this.trRemarks,
    this.trIssuesComments,
    this.trCountry,
    this.trVipStatus,
    this.trTotalCases,
    this.trVipWeight,
    this.trVipLiters,
    this.trVipTotalAmount,
    this.trVipTotalLines,
    this.trQtyProForma,
    this.trAcknowledgeOrder,
    this.trBijlageDone,
    this.trLastUpdateDate,
    this.trRequestedEta,
    this.trDateLoadingShipper,
    this.trDateProFormaReceived,
    this.trFactoryReadyDate,
    this.trEstDepartureDate,
    this.trEstArrivalDate,
    this.trDateArrivalInvoice,
    this.trDateArrivalBol,
    this.trDateArrivalNoteReceived,
    this.trDateManifestReceived,
    this.trDateCopiesToDeclarant,
    this.trDateCustomsPapersReady,
    this.trDateCustomsPapersAsycuda,
    this.trDateContainerAtCps,
    this.trExpirationDateCps,
    this.trDateCustomsPapersToCps,
    this.trDateContainerArrivedLicores,
    this.trDateContainerOpenedCustoms,
    this.trDateContainerUnloadReady,
    this.trReturnDateContainer,
    this.trDateUnloadPapersAdmin,
    this.trCreatedBy,
    this.trCreatedAt,
    this.trUpdatedBy,
    this.trUpdatedAt,
  });

  factory TrackingOrder.fromJson(Map<String, dynamic> j) => TrackingOrder(
        trId: j['TrId'] as int? ?? 0,
        trPoNo: j['TrPoNo'] as String? ?? '',
        trWarehouse: j['TrWarehouse'] as String?,
        trSupplier: j['TrSupplier'] as String?,
        trSupplierCode: j['TrSupplierCode'] as String?,
        trSupplierName: j['TrSupplierName'] as String?,
        trBorw: j['TrBorw'] as String?,
        trOrderDate: j['TrOrderDate'] as int?,
        trVipShipDate: j['TrVipShipDate'] as int?,
        trVipArrivalDate: j['TrVipArrivalDate'] as int?,
        trStatusCode: j['TrStatusCode'] as String?,
        trFreightForwarder: j['TrFreightForwarder'] as String?,
        trContainerNumber: j['TrContainerNumber'] as String?,
        trContainerSize: j['TrContainerSize'] as String?,
        trShippingLine: j['TrShippingLine'] as String?,
        trShippingAgent: j['TrShippingAgent'] as String?,
        trVessel: j['TrVessel'] as String?,
        trConsolidationRef: j['TrConsolidationRef'] as String?,
        trTransitTime: j['TrTransitTime'] as String?,
        trInvoiceNumber: j['TrInvoiceNumber'] as String?,
        trSadNumber: j['TrSadNumber'] as String?,
        trBcNumberOrders: j['TrBcNumberOrders'] as String?,
        trExitNoteNumber: j['TrExitNoteNumber'] as String?,
        trComments: j['TrComments'] as String?,
        trRemarks: j['TrRemarks'] as String?,
        trIssuesComments: j['TrIssuesComments'] as String?,
        trCountry: j['TrCountry'] as String?,
        trVipStatus: j['TrVipStatus'] as String?,
        trTotalCases: (j['TrTotalCases'] as num?)?.toDouble(),
        trVipWeight: (j['TrVipWeight'] as num?)?.toDouble(),
        trVipLiters: (j['TrVipLiters'] as num?)?.toDouble(),
        trVipTotalAmount: (j['TrVipTotalAmount'] as num?)?.toDouble(),
        trVipTotalLines: j['TrVipTotalLines'] as int?,
        trQtyProForma: (j['TrQtyProForma'] as num?)?.toDouble(),
        trAcknowledgeOrder: j['TrAcknowledgeOrder'] as bool?,
        trBijlageDone: j['TrBijlageDone'] as bool?,
        trLastUpdateDate: _dt(j['TrLastUpdateDate']),
        trRequestedEta: _dt(j['TrRequestedEta']),
        trDateLoadingShipper: _dt(j['TrDateLoadingShipper']),
        trDateProFormaReceived: _dt(j['TrDateProFormaReceived']),
        trFactoryReadyDate: _dt(j['TrFactoryReadyDate']),
        trEstDepartureDate: _dt(j['TrEstDepartureDate']),
        trEstArrivalDate: _dt(j['TrEstArrivalDate']),
        trDateArrivalInvoice: _dt(j['TrDateArrivalInvoice']),
        trDateArrivalBol: _dt(j['TrDateArrivalBol']),
        trDateArrivalNoteReceived: _dt(j['TrDateArrivalNoteReceived']),
        trDateManifestReceived: _dt(j['TrDateManifestReceived']),
        trDateCopiesToDeclarant: _dt(j['TrDateCopiesToDeclarant']),
        trDateCustomsPapersReady: _dt(j['TrDateCustomsPapersReady']),
        trDateCustomsPapersAsycuda: _dt(j['TrDateCustomsPapersAsycuda']),
        trDateContainerAtCps: _dt(j['TrDateContainerAtCps']),
        trExpirationDateCps: _dt(j['TrExpirationDateCps']),
        trDateCustomsPapersToCps: _dt(j['TrDateCustomsPapersToCps']),
        trDateContainerArrivedLicores: _dt(j['TrDateContainerArrivedLicores']),
        trDateContainerOpenedCustoms: _dt(j['TrDateContainerOpenedCustoms']),
        trDateContainerUnloadReady: _dt(j['TrDateContainerUnloadReady']),
        trReturnDateContainer: _dt(j['TrReturnDateContainer']),
        trDateUnloadPapersAdmin: _dt(j['TrDateUnloadPapersAdmin']),
        trCreatedBy: j['TrCreatedBy'] as String?,
        trCreatedAt: _dt(j['TrCreatedAt']),
        trUpdatedBy: j['TrUpdatedBy'] as String?,
        trUpdatedAt: _dt(j['TrUpdatedAt']),
      );
}

class TrackingOrderDto {
  final TrackingOrder order;
  final int? daysOverContainer;
  final List<TrackingStatusHistory> statusHistory;

  TrackingOrderDto({
    required this.order,
    this.daysOverContainer,
    required this.statusHistory,
  });

  factory TrackingOrderDto.fromJson(Map<String, dynamic> j) {
    final orderJson = j['Order'] as Map<String, dynamic>?;
    final historyJson = j['StatusHistory'] as List?;
    return TrackingOrderDto(
      order: orderJson != null
          ? TrackingOrder.fromJson(orderJson)
          : TrackingOrder(trId: 0, trPoNo: ''),
      daysOverContainer: j['DaysOverContainer'] as int?,
      statusHistory: historyJson
              ?.map((h) =>
                  TrackingStatusHistory.fromJson(h as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }
}

class OrderStatus {
  final int osId;
  final String osCode;
  final String osDescription;
  final bool isActive;

  OrderStatus({
    required this.osId,
    required this.osCode,
    required this.osDescription,
    required this.isActive,
  });

  factory OrderStatus.fromJson(Map<String, dynamic> j) => OrderStatus(
        osId: j['OsId'] as int? ?? 0,
        osCode: j['OsCode'] as String? ?? '',
        osDescription: j['OsDescription'] as String? ?? '',
        isActive: j['IsActive'] as bool? ?? true,
      );
}

/// Convierte fecha VIP (int YYYYMMDD) a string legible
String fmtVipDate(int? n) {
  if (n == null || n == 0) return '—';
  final s = n.toString().padLeft(8, '0');
  if (s.length != 8) return n.toString();
  return '${s.substring(6, 8)}/${s.substring(4, 6)}/${s.substring(0, 4)}';
}
