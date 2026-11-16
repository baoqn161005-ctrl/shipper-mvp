import '../../shared/domain/order.dart';

abstract interface class CustomerRepository {
  Future<Order> createOrder({
    required String pickupAddress,
    required String pickupPhone,
    required String deliveryAddress,
    required String receiverName,
    required String receiverPhone,
    required String note,
    required double codAmount,
  });
}
