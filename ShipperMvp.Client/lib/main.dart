import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import 'core/network/api_client.dart';
import 'core/storage/token_storage.dart';
import 'features/auth/data/auth_repository_impl.dart';
import 'features/auth/data/auth_remote_data_source.dart';
import 'features/auth/presentation/auth_cubit.dart';
import 'features/auth/presentation/auth_page.dart';
import 'features/customer/data/customer_repository_impl.dart';
import 'features/customer/presentation/create_order_cubit.dart';
import 'features/customer/presentation/create_order_page.dart';
import 'features/shipper/data/shipper_repository_impl.dart';
import 'features/shipper/presentation/order_detail_cubit.dart';
import 'features/shipper/presentation/order_detail_page.dart';
import 'features/shipper/presentation/order_market_cubit.dart';
import 'features/shipper/presentation/order_market_page.dart';
import 'features/shared/domain/order.dart';

void main() {
  final tokenStorage = TokenStorage();
  final apiClient = ApiClient(tokenStorage);
  final authRepository = AuthRepositoryImpl(
    AuthRemoteDataSource(apiClient),
    tokenStorage,
  );
  final shipperRepository = ShipperRepositoryImpl(apiClient);
  final customerRepository = CustomerRepositoryImpl(apiClient);

  runApp(
    ShipperMvpApp(
      authRepository: authRepository,
      shipperRepository: shipperRepository,
      customerRepository: customerRepository,
    ),
  );
}

class ShipperMvpApp extends StatelessWidget {
  const ShipperMvpApp({
    required this.authRepository,
    required this.shipperRepository,
    required this.customerRepository,
    super.key,
  });

  final AuthRepositoryImpl authRepository;
  final ShipperRepositoryImpl shipperRepository;
  final CustomerRepositoryImpl customerRepository;

  @override
  Widget build(BuildContext context) => BlocProvider(
        create: (_) => AuthCubit(authRepository)..restoreSession(),
        child: MaterialApp(
          title: 'Shipper MVP',
          debugShowCheckedModeBanner: false,
          theme: ThemeData(
            colorScheme:
                ColorScheme.fromSeed(seedColor: const Color(0xFF176B5B)),
            useMaterial3: true,
            inputDecorationTheme: const InputDecorationTheme(
              border: OutlineInputBorder(),
            ),
          ),
          home: BlocBuilder<AuthCubit, AuthState>(
            builder: (context, state) {
              if (state is Authenticated) {
                return state.user.role == UserRole.customer
                    ? BlocProvider(
                        create: (_) => CreateOrderCubit(customerRepository),
                        child: const CreateOrderPage(),
                      )
                    : BlocProvider(
                        create: (_) =>
                            OrderMarketCubit(shipperRepository)..refresh(),
                        child: const OrderMarketPage(),
                      );
              }
              if (state is AuthInitial) return const _LoadingPage();
              return const AuthPage();
            },
          ),
          routes: {
            '/create-order': (_) => BlocProvider(
                  create: (_) => CreateOrderCubit(customerRepository),
                  child: const CreateOrderPage(),
                ),
            '/market': (_) => BlocProvider(
                  create: (_) => OrderMarketCubit(shipperRepository)..refresh(),
                  child: const OrderMarketPage(),
                ),
          },
          onGenerateRoute: (settings) {
            if (settings.name == '/order-detail' &&
                settings.arguments is Order) {
              final order = settings.arguments! as Order;
              return MaterialPageRoute<bool>(
                builder: (_) => BlocProvider(
                  create: (_) => OrderDetailCubit(shipperRepository, order),
                  child: const OrderDetailPage(),
                ),
              );
            }
            return null;
          },
        ),
      );
}

class _LoadingPage extends StatelessWidget {
  const _LoadingPage();
  @override
  Widget build(BuildContext context) =>
      const Scaffold(body: Center(child: CircularProgressIndicator()));
}
