import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../auth/presentation/auth_cubit.dart';
import '../../shared/domain/order.dart';
import 'order_market_cubit.dart';

class OrderMarketPage extends StatelessWidget {
  const OrderMarketPage({super.key});

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
          title: const Text('Chợ đơn hàng'),
          actions: [
            IconButton(
              onPressed: () => context.read<AuthCubit>().signOut(),
              icon: const Icon(Icons.logout),
            ),
          ],
        ),
        body: BlocBuilder<OrderMarketCubit, OrderMarketState>(
          builder: (context, state) {
            final orders =
                state is OrderMarketLoaded ? state.orders : <Order>[];
            return RefreshIndicator(
              onRefresh: context.read<OrderMarketCubit>().refresh,
              child: state is OrderMarketLoading
                  ? ListView(
                      children: const [
                        SizedBox(height: 280),
                        Center(child: CircularProgressIndicator()),
                      ],
                    )
                  : orders.isEmpty
                      ? ListView(
                          children: const [
                            SizedBox(height: 180),
                            Icon(Icons.inbox_outlined, size: 56),
                            Center(child: Text('Chưa có đơn đang chờ')),
                          ],
                        )
                      : ListView.separated(
                          padding: const EdgeInsets.all(16),
                          itemCount: orders.length,
                          separatorBuilder: (_, __) =>
                              const SizedBox(height: 10),
                          itemBuilder: (context, index) {
                            final order = orders[index];
                            return Card(
                              child: ListTile(
                                title: Text(
                                  order.orderCode,
                                  style: const TextStyle(
                                      fontWeight: FontWeight.w600),
                                ),
                                subtitle: Text(
                                  '${order.pickupAddress}\n↓  ${order.deliveryAddress}\nCOD: ${order.codAmount.toStringAsFixed(0)}₫',
                                ),
                                isThreeLine: true,
                                trailing: const Icon(Icons.chevron_right),
                                onTap: () async {
                                  final acceptedElsewhere =
                                      await Navigator.pushNamed<bool>(
                                    context,
                                    '/order-detail',
                                    arguments: order,
                                  );
                                  if (acceptedElsewhere == true &&
                                      context.mounted) {
                                    await context
                                        .read<OrderMarketCubit>()
                                        .refresh();
                                  }
                                },
                              ),
                            );
                          },
                        ),
            );
          },
        ),
      );
}
