import '../core/api_client.dart';
import '../models/tracking_order.dart';

class PagedResult<T> {
  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;
  int get totalPages => pageSize > 0 ? (totalCount / pageSize).ceil() : 0;

  PagedResult({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
  });
}

class TrackingService {
  // API usa PropertyNamingPolicy = null → respuestas en PascalCase
  // PagedResponse: { Success, Data: [...], Page, PageSize, TotalCount }

  static Future<PagedResult<TrackingOrderDto>> getOrders({
    String? search,
    String? status,
    String? warehouse,
    String? borw,
    int page = 1,
    int pageSize = 20,
  }) async {
    final params = <String, dynamic>{
      'page': page,
      'pageSize': pageSize,
      if (search != null && search.isNotEmpty) 'search': search,
      if (status != null && status.isNotEmpty) 'status': status,
      if (warehouse != null && warehouse.isNotEmpty) 'warehouse': warehouse,
      if (borw != null && borw.isNotEmpty) 'borw': borw,
    };
    final res = await ApiClient.get('/api/tracking/orders', params: params);
    final rawList = res['Data'] as List? ?? [];
    final items = rawList
        .map((e) => TrackingOrderDto.fromJson(e as Map<String, dynamic>))
        .toList();
    return PagedResult(
      items: items,
      page: res['Page'] as int? ?? page,
      pageSize: res['PageSize'] as int? ?? pageSize,
      totalCount: res['TotalCount'] as int? ?? items.length,
    );
  }

  static Future<TrackingOrderDto> getOrder(int id) async {
    final res = await ApiClient.get('/api/tracking/orders/$id');
    return TrackingOrderDto.fromJson(res['Data'] as Map<String, dynamic>);
  }

  static Future<List<TrackingStatusHistory>> getHistory(int id) async {
    final res = await ApiClient.get('/api/tracking/orders/$id/history');
    final rawList = res['Data'] as List? ?? [];
    return rawList
        .map((e) => TrackingStatusHistory.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  static Future<List<OrderStatus>> getOrderStatuses() async {
    final res = await ApiClient.get('/api/tracking/order-status',
        params: {'pageSize': 100});
    final rawList = res['Data'] as List? ?? [];
    return rawList
        .map((e) => OrderStatus.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  static Future<int> autoImportVip() async {
    final res = await ApiClient.post('/api/tracking/orders/auto-import-vip');
    final data = res['Data'] as Map<String, dynamic>?;
    return data?['Imported'] as int? ?? 0;
  }

  static Future<TrackingOrderDto> syncVip(int id) async {
    final res = await ApiClient.post('/api/tracking/orders/$id/sync-vip');
    return TrackingOrderDto.fromJson(res['Data'] as Map<String, dynamic>);
  }
}
