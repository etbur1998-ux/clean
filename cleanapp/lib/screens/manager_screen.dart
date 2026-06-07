import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/auth_service.dart';
import '../services/api_service.dart';
import '../theme/app_colors.dart';
import '../models/submission_model.dart';
import 'profile_screen.dart';

class ManagerScreen extends StatefulWidget {
  const ManagerScreen({super.key});
  @override
  State<ManagerScreen> createState() => _ManagerScreenState();
}

class _ManagerScreenState extends State<ManagerScreen> {
  final _api = ApiService();
  int _currentIndex = 0;

  late Future<List<SubmissionModel>> _pendingFuture;

  @override
  void initState() {
    super.initState();
    _loadPending();
  }

  void _loadPending() {
    setState(() => _pendingFuture = _api.getPendingSubmissions());
  }

  Future<void> _updateStatus(int id, String status, String receiptType) async {
    final ok = await _api.updateSubmissionStatus(
      id,
      status,
      receiptType: receiptType,
    );
    if (!mounted) return;
    _snack(
      ok ? 'Submission $status' : 'Failed to update status',
      ok ? Colors.green : Colors.red,
    );
    if (ok) _loadPending();
  }

  void _snack(String msg, Color color) =>
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(msg),
          backgroundColor: color,
          behavior: SnackBarBehavior.floating,
        ),
      );

  // ── BUILD ─────────────────────────────────────────────────────────────────
  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final user = Provider.of<AuthService>(context).currentUser!;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Manager Console',
              style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
            ),
            Text(
              user.name,
              style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w400),
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadPending,
            tooltip: 'Refresh',
          ),
        ],
      ),
      body: _currentIndex == 1
          ? const ProfileScreen()
          : _ApprovalsBody(
              pendingFuture: _pendingFuture,
              onUpdateStatus: (id, status, receiptType) =>
                  _updateStatus(id, status, receiptType),
              isDark: isDark,
            ),
      bottomNavigationBar: _ManagerBottomNav(
        currentIndex: _currentIndex,
        isDark: isDark,
        onTap: (i) => setState(() => _currentIndex = i),
      ),
    );
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// APPROVALS BODY
// ══════════════════════════════════════════════════════════════════════════════
class _ApprovalsBody extends StatelessWidget {
  final Future<List<SubmissionModel>> pendingFuture;
  final Future<void> Function(int id, String status, String receiptType)
  onUpdateStatus;
  final bool isDark;

  const _ApprovalsBody({
    required this.pendingFuture,
    required this.onUpdateStatus,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<SubmissionModel>>(
      future: pendingFuture,
      builder: (context, snap) {
        if (snap.connectionState == ConnectionState.waiting)
          return const Center(child: CircularProgressIndicator());

        if (snap.hasError)
          return const Center(child: Text('Error loading pending submissions'));

        if (!snap.hasData || snap.data!.isEmpty)
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(
                  Icons.check_circle_outline,
                  size: 72,
                  color: Colors.green[300],
                ),
                const SizedBox(height: 12),
                const Text(
                  'No pending submissions',
                  style: TextStyle(fontSize: 16, color: Colors.grey),
                ),
                const SizedBox(height: 6),
                const Text(
                  'All caught up!',
                  style: TextStyle(fontSize: 13, color: Colors.grey),
                ),
              ],
            ),
          );

        final items = snap.data!;
        return ListView.builder(
          padding: const EdgeInsets.all(16).copyWith(bottom: 80),
          itemCount: items.length,
          itemBuilder: (_, i) => _PendingCard(
            item: items[i],
            isDark: isDark,
            onApprove: () =>
                onUpdateStatus(items[i].id!, 'Approved', items[i].receiptType),
            onReject: () =>
                onUpdateStatus(items[i].id!, 'Rejected', items[i].receiptType),
          ),
        );
      },
    );
  }
}

// ── Pending Card ──────────────────────────────────────────────────────────────
class _PendingCard extends StatelessWidget {
  final SubmissionModel item;
  final bool isDark;
  final VoidCallback onApprove;
  final VoidCallback onReject;

  const _PendingCard({
    required this.item,
    required this.isDark,
    required this.onApprove,
    required this.onReject,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 2,
      margin: const EdgeInsets.only(bottom: 14),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      color: isDark ? const Color(0xFF1E2D2C) : Colors.white,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  '${item.date}  •  ${item.time}',
                  style: const TextStyle(
                    color: AppColors.textHint,
                    fontSize: 13,
                  ),
                ),
                Row(
                  children: [
                    // Receipt type badge
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 6,
                        vertical: 3,
                      ),
                      margin: const EdgeInsets.only(right: 6),
                      decoration: BoxDecoration(
                        color: item.receiptType == 'Outsource'
                            ? Colors.blue.withValues(alpha: 0.12)
                            : Colors.teal.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(6),
                        border: Border.all(
                          color: item.receiptType == 'Outsource'
                              ? Colors.blue
                              : Colors.teal,
                        ),
                      ),
                      child: Text(
                        item.receiptType,
                        style: TextStyle(
                          color: item.receiptType == 'Outsource'
                              ? Colors.blue
                              : Colors.teal,
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: Colors.orange.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: Colors.orange),
                      ),
                      child: Text(
                        item.status,
                        style: const TextStyle(
                          color: Colors.orange,
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 10),

            // Wereda
            Text(
              item.weredaName ?? 'Unknown Wereda',
              style: TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 15,
                color: isDark ? Colors.white : AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 4),

            // Driver
            Row(
              children: [
                const Icon(
                  Icons.person_outline,
                  size: 14,
                  color: AppColors.textSecondary,
                ),
                const SizedBox(width: 4),
                Text(
                  'Driver: ${item.driverName ?? "Unknown"}',
                  style: const TextStyle(
                    color: AppColors.textSecondary,
                    fontSize: 13,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),

            // KG
            Row(
              children: [
                const Icon(
                  Icons.scale_outlined,
                  size: 14,
                  color: AppColors.textSecondary,
                ),
                const SizedBox(width: 4),
                Text(
                  '${item.kilogram.toStringAsFixed(1)} KG',
                  style: const TextStyle(
                    color: AppColors.textSecondary,
                    fontWeight: FontWeight.bold,
                    fontSize: 13,
                  ),
                ),
              ],
            ),

            if (item.notes.isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(
                'Notes: ${item.notes}',
                style: const TextStyle(
                  color: AppColors.textSecondary,
                  fontSize: 12,
                ),
              ),
            ],

            const SizedBox(height: 14),

            // Action buttons
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton.icon(
                  onPressed: onReject,
                  icon: const Icon(Icons.close, color: Colors.red, size: 18),
                  label: const Text(
                    'Reject',
                    style: TextStyle(color: Colors.red),
                  ),
                ),
                const SizedBox(width: 8),
                ElevatedButton.icon(
                  onPressed: onApprove,
                  icon: const Icon(Icons.check, color: Colors.white, size: 18),
                  label: const Text('Approve'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.green,
                    foregroundColor: Colors.white,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

// ── Bottom Nav ────────────────────────────────────────────────────────────────
class _ManagerBottomNav extends StatelessWidget {
  final int currentIndex;
  final ValueChanged<int> onTap;
  final bool isDark;
  const _ManagerBottomNav({
    required this.currentIndex,
    required this.onTap,
    required this.isDark,
  });

  @override
  Widget build(BuildContext context) {
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
        child: NavigationBar(
          height: 64,
          selectedIndex: currentIndex,
          onDestinationSelected: onTap,
          backgroundColor: Colors.transparent,
          indicatorColor: AppColors.primary.withValues(alpha: 0.15),
          labelBehavior: NavigationDestinationLabelBehavior.alwaysHide,
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.check_circle_outline),
              selectedIcon: Icon(
                Icons.check_circle_rounded,
                color: AppColors.primary,
              ),
              label: 'Approvals',
            ),
            NavigationDestination(
              icon: Icon(Icons.person_outline_rounded),
              selectedIcon: Icon(
                Icons.person_rounded,
                color: AppColors.primary,
              ),
              label: 'Profile',
            ),
          ],
        ),
      ),
    );
  }
}
