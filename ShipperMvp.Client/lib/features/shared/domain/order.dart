enum OrderStatus {
  pending,
  accepted,
  pickedUp,
  delivering,
  completed,
  cancelled,
}

enum UserRole { customer, shipper }

class Order {
  const Order({
    required this.id,
    required this.orderCode,
    required this.pickupAddress,
    required this.deliveryAddress,
    required this.receiverName,
    required this.receiverPhone,
    required this.itemDescription,
    required this.shippingFee,
    required this.codAmount,
    required this.status,
    this.pickupPhone = '',
  });

  final int id;
  final String orderCode;
  final String pickupAddress;
  final String deliveryAddress;
  final String receiverName;
  final String receiverPhone;
  final String itemDescription;
  final String pickupPhone;
  final double shippingFee;
  final double codAmount;
  final OrderStatus status;

  factory Order.fromJson(Map<String, dynamic> json) => Order(
        id: json['id'] as int,
        orderCode: json['orderCode'] as String? ?? 'ORD-${json['id']}',
        pickupAddress: json['pickupAddress'] as String? ?? '',
        deliveryAddress: json['deliveryAddress'] as String? ?? '',
        receiverName: json['receiverName'] as String? ?? '',
        receiverPhone: json['receiverPhone'] as String? ?? '',
        itemDescription: json['itemDescription'] as String? ?? '',
        pickupPhone: json['pickupPhone'] as String? ?? '',
        shippingFee: (json['shippingFee'] as num?)?.toDouble() ?? 0,
        codAmount: (json['codAmount'] as num?)?.toDouble() ?? 0,
        status: OrderStatus.values[(json['status'] as num?)?.toInt() ?? 0],
      );
}

int statusToApiValue(OrderStatus status) => status.index;

String statusLabel(OrderStatus status) => switch (status) {
      OrderStatus.pending => 'Đang chờ shipper',
      OrderStatus.accepted => 'Đã nhận đơn',
      OrderStatus.pickedUp => 'Đã lấy hàng',
      OrderStatus.delivering => 'Đang giao',
      OrderStatus.completed => 'Hoàn thành',
      OrderStatus.cancelled => 'Đã hủy',
    };
