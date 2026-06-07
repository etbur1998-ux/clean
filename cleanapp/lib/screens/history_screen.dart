import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../services/pdf_service.dart';
import '../theme/app_colors.dart';
import '../models/submission_model.dart';

class HistoryScreen extends StatelessWidget {
  const HistoryScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final user       = Provider.of<AuthService>(context).currentUser;
    final apiService = ApiService();

    return FutureBuilder<List<SubmissionModel>>(
      future: apiService.getHistory(user!.id),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (!snapshot.hasData || snapshot.data!.isEmpty) {
          return const Center(child: Text('No submissions found'));
        }
        final history = snapshot.data!;

        return Scaffold(
          backgroundColor: Colors.transparent,
          floatingActionButton: FloatingActionButton(
            tooltip: 'Export to PDF',
            backgroundColor: AppColors.primaryDark,
            onPressed: () async {
              await PdfService().generateAndPrintHistoryPdf(history, user.name);
            },
            child: const Icon(Icons.picture_as_pdf, color: Colors.white),
          ),
          body: ListView.builder(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
            itemCount: history.length,
            itemBuilder: (_, i) => _HistoryCard(item: history[i]),
          ),
        );
      },
    );
  }
}

class _HistoryCard extends StatelessWidget {
  final SubmissionModel item;
  const _HistoryCard({required this.item});

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    Color statusColor;
    switch (item.status.toLowerCase()) {
      case 'approved': statusColor = Colors.green; break;
      case 'rejected': statusColor = Colors.red;   break;
      default:         statusColor = Colors.orange;
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 14),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      color: isDark ? const Color(0xFF1E2D2C) : Colors.white,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('${item.date}  •  ${item.time}',
                  style: const TextStyle(color: AppColors.textHint, fontSize: 13)),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: statusColor),
                ),
                child: Text(item.status,
                    style: TextStyle(color: statusColor, fontSize: 11,
                        fontWeight: FontWeight.bold)),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(children: [
            Container(
              width: 56, height: 56,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(10),
              ),
              child: item.imageUrl != null
                  ? ClipRRect(
                      borderRadius: BorderRadius.circular(10),
                      child: Image.network(item.imageUrl!,
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) =>
                              const Icon(Icons.image, color: Colors.grey)))
                  : const Icon(Icons.inventory_2_outlined,
                      color: AppColors.primary, size: 26),
            ),
            const SizedBox(width: 14),
            Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(item.weredaName ?? 'Unknown Wereda',
                  style: TextStyle(fontWeight: FontWeight.w700, fontSize: 15,
                      color: isDark ? Colors.white : AppColors.textPrimary)),
              if (item.mahberatName != null) ...[
                const SizedBox(height: 2),
                Text(item.mahberatName!,
                    style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
              ],
              const SizedBox(height: 6),
              Row(children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text('${item.kilogram.toStringAsFixed(1)} KG',
                      style: const TextStyle(color: AppColors.primary,
                          fontWeight: FontWeight.w700, fontSize: 12)),
                ),
                const SizedBox(width: 8),
                Text('ETB ${item.total.toStringAsFixed(2)}',
                    style: const TextStyle(color: AppColors.secondaryDark,
                        fontWeight: FontWeight.w700, fontSize: 13)),
              ]),
            ])),
          ]),
        ]),
      ),
    );
  }
}
