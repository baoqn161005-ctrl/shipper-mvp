import '../../shared/domain/order.dart';

abstract interface class ShipperRepository {
  Future<List<Order>> getAvailableOrders();
  Future<Order> acceptOrder(int id);
  Future<Order> updateStatus(int id, OrderStatus status);
}
