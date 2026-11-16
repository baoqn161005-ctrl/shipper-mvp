import 'package:flutter_bloc/flutter_bloc.dart';

import '../../shared/domain/order.dart';
import '../domain/shipper_repository.dart';

sealed class OrderMarketState {}

class OrderMarketLoading extends OrderMarketState {}

class OrderMarketLoaded extends OrderMarketState {
  OrderMarketLoaded(this.orders, {this.refreshing = false, this.error});
  final List<Order> orders;
  final bool refreshing;
  final String? error;
}

class OrderMarketCubit extends Cubit<OrderMarketState> {
  OrderMarketCubit(this._repository) : super(OrderMarketLoading());
  final ShipperRepository _repository;

  Future<void> refresh() async {
    final previous = state is OrderMarketLoaded
        ? (state as OrderMarketLoaded).orders
        : <Order>[];
    emit(OrderMarketLoaded(previous, refreshing: true));
    try {
      emit(OrderMarketLoaded(await _repository.getAvailableOrders()));
    } catch (error) {
      emit(OrderMarketLoaded(previous, error: error.toString()));
    }
  }
}
