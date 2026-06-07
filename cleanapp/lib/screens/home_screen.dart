import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:go_router/go_router.dart';
import '../services/auth_service.dart';
import '../services/api_service.dart';
import '../services/notification_service.dart';
import '../theme/app_colors.dart';
import '../models/submission_model.dart';
import 'history_screen.dart';
import 'profile_screen.dart';
import 'report_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _currentIndex = 0;

  @override
  void initState() {
    super.initState();
    // Start notification polling
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final user = Provider.of<AuthService>(context, listen: false).currentUser;
      if (user != null) {
        Provider.of<NotificationService>(
          context,
          listen: false,
        ).startPolling(user.id);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final user = Provider.of<AuthService>(context).currentUser;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    if (user == null)
      return const Scaffold(body: Center(child: CircularProgressIndicator()));

    // Role-based screens — driver gets full dashboard
    final screens = [
      _Dashboard(isDark: isDark),
      ReportScreen(),
      HistoryScreen(),
      ProfileScreen(),
    ];

    return Scaffold(
      backgroundColor: isDark ? const Color(0xFF111A19) : AppColors.background,
      extendBodyBehindAppBar: true,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Welcome back,',
              style: TextStyle(
                fontSize: 12,
                color: isDark ? Colors.white54 : AppColors.textSecondary,
              ),
            ),
            Text(
              user.name,
              style: TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w700,
                color: isDark ? Colors.white : AppColors.textPrimary,
              ),
            ),
          ],
        ),
        actions: [
          Consumer<NotificationService>(
            builder: (_, ns, __) => Stack(
              children: [
                IconButton(
                  icon: Container(
                    padding: const EdgeInsets.all(6),
                    decoration: BoxDecoration(
                      color: isDark
                          ? Colors.white.withValues(alpha: 0.1)
                          : AppColors.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Icon(
                      Icons.notifications_outlined,
                      color: isDark ? Colors.white70 : AppColors.primary,
                      size: 22,
                    ),
                  ),
                  onPressed: () => context.push('/notifications'),
                ),
                if (ns.unreadCount > 0)
                  Positioned(
                    top: 8,
                    right: 8,
                    child: Container(
                      width: 16,
                      height: 16,
                      decoration: const BoxDecoration(
                        color: Colors.red,
                        shape: BoxShape.circle,
                      ),
                      child: Center(
                        child: Text(
                          ns.unreadCount > 9 ? '9+' : '${ns.unreadCount}',
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 9,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
      body: screens[_currentIndex],
      bottomNavigationBar: _BottomNav(
        currentIndex: _currentIndex,
        onTap: (i) => setState(() => _currentIndex = i),
        isDark: isDark,
      ),
    );
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// DASHBOARD TAB
// ══════════════════════════════════════════════════════════════════════════════
class _Dashboard extends StatelessWidget {
  final bool isDark;
  const _Dashboard({required this.isDark});

  @override
  Widget build(BuildContext context) {
    final user = Provider.of<AuthService>(context, listen: false).currentUser!;

    return FutureBuilder<List<SubmissionModel>>(
      future: ApiService().getHistory(user.id),
      builder: (context, snap) {
        final history = snap.data ?? [];
        final now = DateTime.now();
        double todayKg = 0, weekKg = 0, monthKg = 0;
        final spots = List.generate(7, (i) => FlSpot(i.toDouble(), 0));

        for (final item in history) {
          try {
            final d = DateTime.parse(item.date);
            final diff = now.difference(d).inDays;
            if (d.year == now.year && d.month == now.month && d.day == now.day)
              todayKg += item.kilogram;
            if (d.year == now.year && d.month == now.month)
              monthKg += item.kilogram;
            if (diff >= 0 && diff < 7) {
              weekKg += item.kilogram;
              final si = 6 - diff;
              spots[si] = FlSpot(si.toDouble(), spots[si].y + item.kilogram);
            }
          } catch (_) {}
        }

        return CustomScrollView(
          slivers: [
            // Hero header
            SliverToBoxAdapter(
              child: Container(
                padding: EdgeInsets.fromLTRB(
                  20,
                  MediaQuery.of(context).padding.top + 70,
                  20,
                  28,
                ),
                decoration: const BoxDecoration(
                  gradient: AppColors.cardGradient,
                  borderRadius: BorderRadius.vertical(
                    bottom: Radius.circular(28),
                  ),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Driver console banner
                    if (user.isDriver) ...[
                      Container(
                        margin: const EdgeInsets.only(bottom: 20),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 14,
                          vertical: 12,
                        ),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(14),
                        ),
                        child: Row(
                          children: [
                            const Icon(
                              Icons.local_shipping_rounded,
                              color: Colors.white,
                              size: 22,
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'Driver Console',
                                    style: TextStyle(
                                      color: Colors.white,
                                      fontWeight: FontWeight.w700,
                                      fontSize: 13,
                                    ),
                                  ),
                                  Text(
                                    user.vehicleName ?? 'No vehicle assigned',
                                    style: TextStyle(
                                      color: Colors.white.withValues(
                                        alpha: 0.75,
                                      ),
                                      fontSize: 12,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            GestureDetector(
                              onTap: () => context.push('/driver-trips'),
                              child: Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 12,
                                  vertical: 6,
                                ),
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  borderRadius: BorderRadius.circular(20),
                                ),
                                child: const Text(
                                  'View Jobs',
                                  style: TextStyle(
                                    color: AppColors.primaryDark,
                                    fontWeight: FontWeight.w700,
                                    fontSize: 12,
                                  ),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ).animate().fadeIn(duration: 300.ms).slideX(begin: -0.1),
                    ],

                    const Text(
                      'Performance Overview',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 18,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 16),

                    // Responsive stats — use LayoutBuilder
                    LayoutBuilder(
                      builder: (ctx, constraints) {
                        final isWide = constraints.maxWidth > 500;
                        return isWide
                            ? Row(
                                children: [
                                  Expanded(
                                    child: _MiniStat(
                                      'Today',
                                      todayKg,
                                      Icons.today_rounded,
                                      Colors.white,
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    child: _MiniStat(
                                      'This Week',
                                      weekKg,
                                      Icons.date_range_rounded,
                                      Colors.white70,
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    child: _MiniStat(
                                      'This Month',
                                      monthKg,
                                      Icons.calendar_month_rounded,
                                      Colors.white60,
                                    ),
                                  ),
                                ],
                              )
                            : Column(
                                children: [
                                  Row(
                                    children: [
                                      Expanded(
                                        child: _MiniStat(
                                          'Today',
                                          todayKg,
                                          Icons.today_rounded,
                                          Colors.white,
                                        ),
                                      ),
                                      const SizedBox(width: 8),
                                      Expanded(
                                        child: _MiniStat(
                                          'Week',
                                          weekKg,
                                          Icons.date_range_rounded,
                                          Colors.white70,
                                        ),
                                      ),
                                    ],
                                  ),
                                  const SizedBox(height: 8),
                                  _MiniStat(
                                    'This Month',
                                    monthKg,
                                    Icons.calendar_month_rounded,
                                    Colors.white60,
                                  ),
                                ],
                              );
                      },
                    ),
                  ],
                ),
              ),
            ),

            // Quick Actions
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 24, 20, 0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Quick Actions',
                      style: TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: isDark ? Colors.white : AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 12),
                    _QuickActions(user: user, isDark: isDark),
                  ],
                ),
              ),
            ),

            // Chart
            SliverToBoxAdapter(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 24, 20, 0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Last 7 Days (KG)',
                      style: TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w700,
                        color: isDark ? Colors.white : AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 14),
                    Container(
                          height: 180,
                          padding: const EdgeInsets.fromLTRB(8, 16, 16, 8),
                          decoration: BoxDecoration(
                            color: isDark
                                ? const Color(0xFF1E2D2C)
                                : Colors.white,
                            borderRadius: BorderRadius.circular(20),
                            boxShadow: [
                              BoxShadow(
                                color: isDark
                                    ? Colors.black26
                                    : AppColors.shadow,
                                blurRadius: 10,
                                offset: const Offset(0, 4),
                              ),
                            ],
                          ),
                          child: snap.connectionState == ConnectionState.waiting
                              ? const Center(child: CircularProgressIndicator())
                              : LineChart(
                                  LineChartData(
                                    gridData: FlGridData(
                                      show: true,
                                      drawVerticalLine: false,
                                      horizontalInterval: 50,
                                      getDrawingHorizontalLine: (_) => FlLine(
                                        color: isDark
                                            ? Colors.white10
                                            : Colors.grey[200]!,
                                        strokeWidth: 1,
                                      ),
                                    ),
                                    titlesData: FlTitlesData(
                                      rightTitles: AxisTitles(
                                        sideTitles: SideTitles(
                                          showTitles: false,
                                        ),
                                      ),
                                      topTitles: AxisTitles(
                                        sideTitles: SideTitles(
                                          showTitles: false,
                                        ),
                                      ),
                                      leftTitles: AxisTitles(
                                        sideTitles: SideTitles(
                                          showTitles: true,
                                          reservedSize: 32,
                                          getTitlesWidget: (v, _) => Text(
                                            v.toInt().toString(),
                                            style: TextStyle(
                                              fontSize: 9,
                                              color: isDark
                                                  ? Colors.white38
                                                  : Colors.grey[400],
                                            ),
                                          ),
                                        ),
                                      ),
                                      bottomTitles: AxisTitles(
                                        sideTitles: SideTitles(
                                          showTitles: true,
                                          getTitlesWidget: (v, _) {
                                            final d = DateTime.now().subtract(
                                              Duration(days: 6 - v.toInt()),
                                            );
                                            const days = [
                                              'M',
                                              'T',
                                              'W',
                                              'T',
                                              'F',
                                              'S',
                                              'S',
                                            ];
                                            return Text(
                                              days[d.weekday - 1],
                                              style: TextStyle(
                                                fontSize: 10,
                                                fontWeight: FontWeight.w600,
                                                color: isDark
                                                    ? Colors.white38
                                                    : Colors.grey[500],
                                              ),
                                            );
                                          },
                                        ),
                                      ),
                                    ),
                                    borderData: FlBorderData(show: false),
                                    lineBarsData: [
                                      LineChartBarData(
                                        spots: spots,
                                        isCurved: true,
                                        curveSmoothness: 0.3,
                                        color: AppColors.primary,
                                        barWidth: 3,
                                        isStrokeCapRound: true,
                                        dotData: FlDotData(
                                          show: true,
                                          getDotPainter: (s, _, __, ___) =>
                                              FlDotCirclePainter(
                                                radius: 3,
                                                color: Colors.white,
                                                strokeWidth: 2,
                                                strokeColor: AppColors.primary,
                                              ),
                                        ),
                                        belowBarData: BarAreaData(
                                          show: true,
                                          gradient: LinearGradient(
                                            colors: [
                                              AppColors.primary.withValues(
                                                alpha: 0.25,
                                              ),
                                              AppColors.primary.withValues(
                                                alpha: 0.0,
                                              ),
                                            ],
                                            begin: Alignment.topCenter,
                                            end: Alignment.bottomCenter,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                        )
                        .animate()
                        .fadeIn(delay: 200.ms)
                        .scale(curve: Curves.easeOutBack),
                  ],
                ),
              ),
            ),

            // Recent history
            if (history.isNotEmpty) ...[
              SliverToBoxAdapter(
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(20, 24, 20, 8),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'Recent Submissions',
                        style: TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.w700,
                          color: isDark ? Colors.white : AppColors.textPrimary,
                        ),
                      ),
                      TextButton(
                        onPressed: () {},
                        child: const Text(
                          'See All',
                          style: TextStyle(color: AppColors.primary),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              SliverList(
                delegate: SliverChildBuilderDelegate(
                  (ctx, i) => _RecentTile(
                    item: history[i],
                    isDark: isDark,
                  ).animate().fadeIn(delay: (i * 60).ms).slideX(begin: 0.1),
                  childCount: history.length.clamp(0, 5),
                ),
              ),
            ],
            const SliverToBoxAdapter(child: SizedBox(height: 100)),
          ],
        );
      },
    );
  }
}

// ── Quick Actions ─────────────────────────────────────────────────────────────
class _QuickActions extends StatelessWidget {
  final dynamic user;
  final bool isDark;
  const _QuickActions({required this.user, required this.isDark});

  @override
  Widget build(BuildContext context) {
    final actions = <Map<String, dynamic>>[
      if (user.isDriver) ...[
        {
          'label': 'My Trips',
          'icon': Icons.local_shipping_rounded,
          'color': Colors.teal,
          'route': '/driver-trips',
        },
        {
          'label': 'Notifications',
          'icon': Icons.notifications_rounded,
          'color': Colors.orange,
          'route': '/notifications',
        },
      ],
      if (!user.isDriver) ...[
        {
          'label': 'History',
          'icon': Icons.history_rounded,
          'color': Colors.blue,
          'route': null,
          'tab': 2,
        },
        {
          'label': 'Reports',
          'icon': Icons.analytics_rounded,
          'color': Colors.purple,
          'route': null,
          'tab': 1,
        },
        {
          'label': 'Notifications',
          'icon': Icons.notifications_rounded,
          'color': Colors.orange,
          'route': '/notifications',
        },
      ],
    ];

    if (actions.isEmpty) return const SizedBox.shrink();

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
        maxCrossAxisExtent: 140,
        mainAxisExtent: 90,
        crossAxisSpacing: 10,
        mainAxisSpacing: 10,
      ),
      itemCount: actions.length,
      itemBuilder: (ctx, i) {
        final a = actions[i];
        return GestureDetector(
          onTap: () {
            if (a['route'] != null) ctx.push(a['route'] as String);
          },
          child: Container(
            decoration: BoxDecoration(
              color: isDark ? const Color(0xFF1E2D2C) : Colors.white,
              borderRadius: BorderRadius.circular(14),
              boxShadow: [BoxShadow(color: AppColors.shadow, blurRadius: 6)],
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: (a['color'] as Color).withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(
                    a['icon'] as IconData,
                    color: a['color'] as Color,
                    size: 22,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  a['label'] as String,
                  style: TextStyle(
                    fontSize: 11.5,
                    fontWeight: FontWeight.w600,
                    color: isDark ? Colors.white : AppColors.textPrimary,
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

// ── Mini stat ──────────────────────────────────────────────────────────────────
class _MiniStat extends StatelessWidget {
  final String label;
  final double value;
  final IconData icon;
  final Color color;
  const _MiniStat(this.label, this.value, this.icon, this.color);

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 12),
    decoration: BoxDecoration(
      color: Colors.white.withValues(alpha: 0.15),
      borderRadius: BorderRadius.circular(14),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, color: color, size: 18),
        const SizedBox(height: 6),
        Text(
          value.toStringAsFixed(1),
          style: TextStyle(
            color: color,
            fontSize: 20,
            fontWeight: FontWeight.w800,
          ),
        ),
        Text(
          label,
          style: TextStyle(color: color.withValues(alpha: 0.75), fontSize: 10),
        ),
      ],
    ),
  );
}

// ── Recent tile ────────────────────────────────────────────────────────────────
class _RecentTile extends StatelessWidget {
  final SubmissionModel item;
  final bool isDark;
  const _RecentTile({required this.item, required this.isDark});

  @override
  Widget build(BuildContext context) {
    final statusColor = item.status == 'Approved'
        ? Colors.green
        : item.status == 'Rejected'
        ? Colors.red
        : Colors.orange;
    return Container(
      margin: const EdgeInsets.fromLTRB(20, 0, 20, 10),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF1E2D2C) : Colors.white,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [
          BoxShadow(
            color: isDark ? Colors.black12 : AppColors.shadow,
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Icon(
              Icons.inventory_2_outlined,
              color: AppColors.primary,
              size: 22,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.weredaName ?? 'Receipt',
                  style: TextStyle(
                    fontWeight: FontWeight.w600,
                    fontSize: 14,
                    color: isDark ? Colors.white : AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  '${item.kilogram.toStringAsFixed(1)} KG  •  ${item.date}',
                  style: TextStyle(
                    fontSize: 12,
                    color: isDark ? Colors.white54 : AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: statusColor.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Text(
              item.status,
              style: TextStyle(
                color: statusColor,
                fontSize: 10,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// BOTTOM NAV
// ══════════════════════════════════════════════════════════════════════════════
class _BottomNav extends StatelessWidget {
  final int currentIndex;
  final ValueChanged<int> onTap;
  final bool isDark;
  const _BottomNav({
    required this.currentIndex,
    required this.onTap,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    const destinations = [
      NavigationDestination(
        icon: Icon(Icons.dashboard_outlined),
        selectedIcon: Icon(Icons.dashboard_rounded, color: AppColors.primary),
        label: 'Home',
      ),
      NavigationDestination(
        icon: Icon(Icons.analytics_outlined),
        selectedIcon: Icon(Icons.analytics_rounded, color: AppColors.primary),
        label: 'Report',
      ),
      NavigationDestination(
        icon: Icon(Icons.history_outlined),
        selectedIcon: Icon(Icons.history_rounded, color: AppColors.primary),
        label: 'History',
      ),
      NavigationDestination(
        icon: Icon(Icons.person_outline_rounded),
        selectedIcon: Icon(Icons.person_rounded, color: AppColors.primary),
        label: 'Profile',
      ),
    ];

    return Container(
      margin: const EdgeInsets.fromLTRB(16, 0, 16, 16),
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF1E2D2C) : Colors.white,
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: isDark ? Colors.black38 : AppColors.shadow,
            blurRadius: 20,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(24),
        child: BackdropFilter(
          filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
          child: NavigationBar(
            height: 64,
            selectedIndex: currentIndex,
            onDestinationSelected: onTap,
            backgroundColor: Colors.transparent,
            indicatorColor: AppColors.primary.withValues(alpha: 0.15),
            labelBehavior: NavigationDestinationLabelBehavior.alwaysHide,
            destinations: destinations,
          ),
        ),
      ),
    );
  }
}
