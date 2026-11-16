import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../shared/domain/order.dart';
import 'order_detail_cubit.dart';

class OrderDetailPage extends StatelessWidget {
  const OrderDetailPage({super.key});

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Chi tiết đơn hàng')),
        body: BlocConsumer<OrderDetailCubit, OrderDetailState>(
          listener: (context, state) {
            if (state is OrderDetailReady && state.error != null) {
              ScaffoldMessenger.of(
                context,
              ).showSnackBar(SnackBar(content: Text(state.error!)));
              if (state.acceptConflict) {
                Navigator.of(context).pop(true);
              }
            }
          },
          builder: (context, state) {
            final ready = state as OrderDetailReady;
            final order = ready.order;
            final isPending = order.status == OrderStatus.pending;
            final nextTitle = switch (order.status) {
              OrderStatus.accepted => 'Xác nhận đã lấy hàng',
              OrderStatus.pickedUp => 'Bắt đầu giao hàng',
              OrderStatus.delivering => 'Hoàn thành giao hàng',
              OrderStatus.completed => 'Đã hoàn thành',
              _ => 'Đơn đã đóng',
            };
            return ListView(
              padding: const EdgeInsets.all(20),
              children: [
                Text(
                  order.orderCode,
                  style: Theme.of(context).textTheme.headlineSmall,
                ),
                const SizedBox(height: 8),
                Chip(label: Text(statusLabel(order.status))),
                const SizedBox(height: 16),
                _info(
                  context,
                  Icons.trip_origin,
                  'Điểm lấy hàng',
                  order.pickupAddress,
                ),
                _info(
                  context,
                  Icons.location_on_outlined,
                  'Điểm giao hàng',
                  order.deliveryAddress,
                ),
                _info(
                  context,
                  Icons.person_outline,
                  'Người nhận',
                  '${order.receiverName} · ${order.receiverPhone}',
                ),
                _info(
                  context,
                  Icons.inventory_2_outlined,
                  'Ghi chú',
                  order.itemDescription,
                ),
                _info(
                  context,
                  Icons.payments_outlined,
                  'COD',
                  '${order.codAmount.toStringAsFixed(0)}₫',
                ),
                const SizedBox(height: 28),
                if (order.status != OrderStatus.completed &&
                    order.status != OrderStatus.cancelled)
                  FilledButton(
                    onPressed: ready.busy
                        ? null
                        : () => isPending
                            ? context.read<OrderDetailCubit>().accept()
                            : context.read<OrderDetailCubit>().advance(),
                    child: ready.busy
                        ? const SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Text(isPending ? 'Nhận đơn' : nextTitle),
                  ),
              ],
            );
          },
        ),
      );

  Widget _info(
    BuildContext context,
    IconData icon,
    String label,
    String value,
  ) =>
      Padding(
        padding: const EdgeInsets.symmetric(vertical: 10),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, size: 20),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: Theme.of(context).textTheme.labelMedium),
                  const SizedBox(height: 4),
                  Text(value),
                ],
              ),
            ),
          ],
        ),
      );
}
