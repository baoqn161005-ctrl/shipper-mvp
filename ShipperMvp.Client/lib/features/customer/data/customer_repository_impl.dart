import '../../../core/network/api_client.dart';
import '../../shared/domain/order.dart';
import '../domain/customer_repository.dart';

class CustomerRepositoryImpl implements CustomerRepository {
  CustomerRepositoryImpl(this._client);
  final ApiClient _client;

  @override
  Future<Order> createOrder({
    required String pickupAddress,
    required String pickupPhone,
    required String deliveryAddress,
    required String receiverName,
    required String receiverPhone,
    required String note,
    required double codAmount,
  }) async {
    final response = await _client.dio.post<Map<String, dynamic>>(
      '/api/orders',
      data: {
        'pickupAddress': pickupAddress,
        'pickupPhone': pickupPhone,
        'deliveryAddress': deliveryAddress,
        'receiverName': receiverName,
        'receiverPhone': receiverPhone,
        'itemDescription': note,
        'shippingFee': 25000,
        'codAmount': codAmount,
      },
    );
    return Order.fromJson(response.data!);
  }
}
