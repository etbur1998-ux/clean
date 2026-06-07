import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import '../models/notification_model.dart';
import '../services/notification_service.dart';
import '../services/auth_service.dart';
import '../theme/app_colors.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});
  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _init());
  }

  void _init() {
    final ns = Provider.of<NotificationService>(context, listen: false);
    final user = Provider.of<AuthService>(context, listen: false).currentUser;
    if (user != null) ns.startPolling(user.id);
  }

  @override
  void dispose() {
    Provider.of<NotificationService>(context, listen: false).stopPolling();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Scaffold(
      backgroundColor: isDark ? const Color(0xFF111A19) : AppColors.background,
      appBar: AppBar(
        backgroundColor: isDark ? const Color(0xFF1E2D2C) : AppColors.surface,
        foregroundColor: isDark ? Colors.white : AppColors.textPrimary,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () => Navigator.canPop(context)
              ? Navigator.pop(context)
              : context.go('/home'),
        ),
        title: const Text(
          'Notifications',
          style: TextStyle(fontWeight: FontWeight.w800),
        ),
        actions: [
          Consumer<NotificationService>(
            builder: (_, ns, __) => ns.unreadCount > 0
                ? TextButton.icon(
                    onPressed: () async {
                      final user = Provider.of<AuthService>(
                        context,
                        listen: false,
                      ).currentUser;
                      if (user != null) await ns.fetchNotifications(user.id);
                    },
                    icon: const Icon(Icons.done_all, size: 16),
                    label: const Text(
                      'Mark all read',
                      style: TextStyle(fontSize: 12),
                    ),
                  )
                : const SizedBox.shrink(),
          ),
        ],
      ),
      body: Consumer<NotificationService>(
        builder: (_, ns, __) {
          if (ns.isLoading && ns.notifications.isEmpty)
            return const Center(child: CircularProgressIndicator());

          if (ns.notifications.isEmpty)
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.notifications_none_rounded,
                    size: 72,
                    color: Colors.grey[300],
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'All caught up!',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'No notifications yet',
                    style: TextStyle(color: Colors.grey[500]),
                  ),
                ],
              ),
            );

          return RefreshIndicator(
            onRefresh: () async {
              final user = Provider.of<AuthService>(
                context,
                listen: false,
              ).currentUser;
              if (user != null) await ns.fetchNotifications(user.id);
            },
            child: ListView.separated(
              padding: const EdgeInsets.all(12),
              itemCount: ns.notifications.length,
              separatorBuilder: (_, __) => const SizedBox(height: 6),
              itemBuilder: (_, i) => _NotifCard(
                notif: ns.notifications[i],
                isDark: isDark,
                onTap: () async {
                  final notif = ns.notifications[i];
                  await ns.markAsRead(notif);
                  if (notif.notificationType == 'Action' && context.mounted) {
                    context.push('/driver-trips');
                  }
                },
              ),
            ),
          );
        },
      ),
    );
  }
}

class _NotifCard extends StatelessWidget {
  final NotificationModel notif;
  final bool isDark;
  final VoidCallback onTap;
  const _NotifCard({
    required this.notif,
    required this.isDark,
    required this.onTap,
  });

  Color get _color => switch (notif.notificationType) {
    'Action' => Colors.orange,
    'Success' => Colors.green,
    'Warning' => Colors.red,
    _ => Colors.blue,
  };

  IconData get _icon => switch (notif.notificationType) {
    'Action' => Icons.settings_rounded,
    'Success' => Icons.check_circle_rounded,
    'Warning' => Icons.warning_amber_rounded,
    _ => Icons.info_rounded,
  };

  @override
  Widget build(BuildContext context) {
    return Card(
      color: notif.isRead
          ? (isDark ? const Color(0xFF1E2D2C) : Colors.white)
          : (isDark ? const Color(0xFF243B38) : _color.withValues(alpha: 0.04)),
      elevation: notif.isRead ? 0 : 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: notif.isRead
            ? BorderSide.none
            : BorderSide(color: _color.withValues(alpha: 0.3), width: 1),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(14),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: _color.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(_icon, color: _color, size: 22),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            notif.title,
                            style: TextStyle(
                              fontWeight: FontWeight.w700,
                              fontSize: 14,
                              color: isDark
                                  ? Colors.white
                                  : AppColors.textPrimary,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        if (!notif.isRead)
                          Container(
                            width: 8,
                            height: 8,
                            decoration: BoxDecoration(
                              color: _color,
                              shape: BoxShape.circle,
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      notif.body,
                      style: TextStyle(
                        fontSize: 12.5,
                        color: isDark
                            ? Colors.white60
                            : AppColors.textSecondary,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        Icon(
                          Icons.access_time_rounded,
                          size: 11,
                          color: Colors.grey[400],
                        ),
                        const SizedBox(width: 3),
                        Text(
                          _timeAgo(notif.createdAt),
                          style: TextStyle(
                            fontSize: 11,
                            color: Colors.grey[400],
                          ),
                        ),
                        if (notif.requestNumber != null) ...[
                          const SizedBox(width: 8),
                          Text(
                            '• ${notif.requestNumber}',
                            style: TextStyle(
                              fontSize: 11,
                              color: _color,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  String _timeAgo(DateTime dt) {
    final diff = DateTime.now().difference(dt);
    if (diff.inSeconds < 60) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    return '${diff.inDays}d ago';
  }
}
