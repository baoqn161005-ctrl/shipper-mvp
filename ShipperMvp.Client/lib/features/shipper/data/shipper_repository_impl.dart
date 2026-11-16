import 'package:dio/dio.dart';

import '../../../core/network/api_client.dart';
import '../../shared/domain/order.dart';
import '../domain/shipper_repository.dart';

class ShipperRepositoryImpl implements ShipperRepository {
  ShipperRepositoryImpl(this._client);
  final ApiClient _client;

  @override
  Future<List<Order>> getAvailableOrders() async {
    final response = await _client.dio.get<List<dynamic>>(
      '/api/orders/available',
    );
    return (response.data ?? [])
        .map((item) => Order.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  @override
  Future<Order> acceptOrder(int id) async => _order(
        await _client.dio.post<Map<String, dynamic>>('/api/orders/$id/accept'),
      );

  @override
  Future<Order> updateStatus(int id, OrderStatus status) async => _order(
        await _client.dio.put<Map<String, dynamic>>(
          '/api/orders/$id/status',
          data: {'newStatus': statusToApiValue(status)},
        ),
      );

  Order _order(Response<Map<String, dynamic>> response) =>
      Order.fromJson(response.data!);
}
