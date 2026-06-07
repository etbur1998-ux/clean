import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';
import 'theme/app_colors.dart';
import 'theme/theme_provider.dart';
import 'services/auth_service.dart';
import 'services/notification_service.dart';
import 'screens/login_screen.dart';
import 'screens/home_screen.dart';
import 'screens/notifications_screen.dart';
import 'screens/trip_details_screen.dart';
import 'screens/driver_trips_screen.dart';
import 'screens/private_company_screen.dart';
import 'screens/outsource_screen.dart';
import 'screens/manager_screen.dart';

void main() {
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => ThemeProvider()),
        ChangeNotifierProvider(create: (_) => AuthService()),
        ChangeNotifierProvider(create: (_) => NotificationService()),
      ],
      child: const CleanApp(),
    ),
  );
}

class CleanApp extends StatelessWidget {
  const CleanApp({super.key});

  @override
  Widget build(BuildContext context) {
    final authService = Provider.of<AuthService>(context, listen: false);

    final _router = GoRouter(
      initialLocation: '/login',
      refreshListenable: authService,
      redirect: (context, state) {
        final isAuthenticated = authService.isAuthenticated;
        final isLoggingIn = state.matchedLocation == '/login';

        if (!isAuthenticated && !isLoggingIn) return '/login';
        if (isAuthenticated && isLoggingIn) {
          final role = authService.currentUser?.role.toLowerCase() ?? '';
          if (role == 'privatecompanyrep') return '/private-company';
          if (role == 'outsource') return '/outsource';
          if (role == 'manager') return '/manager';
          return '/home';
        }
        return null;
      },
      routes: [
        GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
        GoRoute(path: '/home', builder: (_, __) => const HomeScreen()),
        GoRoute(
          path: '/private-company',
          builder: (_, __) => const PrivateCompanyScreen(),
        ),
        GoRoute(
          path: '/outsource',
          builder: (_, __) => const OutsourceScreen(),
        ),
        GoRoute(path: '/manager', builder: (_, __) => const ManagerScreen()),
        GoRoute(
          path: '/notifications',
          builder: (_, __) => const NotificationsScreen(),
        ),
        GoRoute(
          path: '/driver-trips',
          builder: (_, __) => const DriverTripsScreen(),
        ),
        GoRoute(
          path: '/trip-details/:requestId',
          builder: (context, state) {
            final requestId =
                int.tryParse(state.pathParameters['requestId'] ?? '0') ?? 0;
            final extra = state.extra as Map<String, dynamic>?;
            final requestNumber = extra?['requestNumber'] as String? ?? '';
            return TripDetailsScreen(
              requestId: requestId,
              requestNumber: requestNumber,
            );
          },
        ),
      ],
    );

    return Consumer<ThemeProvider>(
      builder: (context, themeProvider, child) {
        return MaterialApp.router(
          title: 'CleanApp',
          debugShowCheckedModeBanner: false,
          routerConfig: _router,
          themeMode: themeProvider.themeMode,
          theme: ThemeData(
            useMaterial3: true,
            brightness: Brightness.light,
            colorScheme: ColorScheme.fromSeed(
              brightness: Brightness.light,
              seedColor: AppColors.primary,
              primary: AppColors.primary,
              secondary: AppColors.secondary,
              surface: AppColors.surface,
              error: AppColors.error,
            ),
            fontFamily: 'Inter',
            scaffoldBackgroundColor: AppColors.background,
            appBarTheme: const AppBarTheme(
              centerTitle: false,
              elevation: 0,
              backgroundColor: AppColors.surface,
              foregroundColor: AppColors.textPrimary,
            ),
            cardTheme: CardThemeData(
              elevation: 0,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(16),
              ),
            ),
          ),
          darkTheme: ThemeData(
            useMaterial3: true,
            brightness: Brightness.dark,
            colorScheme: ColorScheme.fromSeed(
              brightness: Brightness.dark,
              seedColor: AppColors.primary,
              primary: AppColors.primary,
              secondary: AppColors.secondary,
              surface: const Color(0xFF1E2D2C),
              error: AppColors.error,
            ),
            fontFamily: 'Inter',
            scaffoldBackgroundColor: const Color(0xFF111A19),
            appBarTheme: const AppBarTheme(
              centerTitle: false,
              elevation: 0,
              backgroundColor: Color(0xFF1E2D2C),
              foregroundColor: Colors.white,
            ),
            cardTheme: CardThemeData(
              elevation: 0,
              color: const Color(0xFF1E2D2C),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(16),
              ),
            ),
          ),
        );
      },
    );
  }
}
