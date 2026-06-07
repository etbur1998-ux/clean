import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:intl/intl.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../services/auth_service.dart';
import '../services/api_service.dart';
import '../services/pdf_service.dart';
import '../models/submission_model.dart';
import '../theme/app_colors.dart';

class ReportScreen extends StatefulWidget {
  const ReportScreen({super.key});
  @override
  State<ReportScreen> createState() => _ReportScreenState();
}

class _ReportScreenState extends State<ReportScreen> {
  final ApiService _apiService = ApiService();
  DateTime _startDate = DateTime.now().subtract(const Duration(days: 30));
  DateTime _endDate = DateTime.now();
  bool _isLoading = false;
  
  List<dynamic> _reportData = [];
  double _totalKg = 0;
  double _totalCost = 0;
  double _avgKg = 0;
  int _completedCount = 0;

  @override
  void initState() {
    super.initState();
    _fetchReport();
  }

  Future<void> _fetchReport() async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser;
    if (user == null) return;

    setState(() => _isLoading = true);

    final startStr = DateFormat('yyyy-MM-dd').format(_startDate);
    final endStr = DateFormat('yyyy-MM-dd').format(_endDate);

    final reportMap = await _apiService.getReportData(user.id, startDate: startStr, endDate: endStr);
    final rawData = reportMap['data'] as List<dynamic>? ?? [];

    double tempTotalKg = 0;
    double tempTotalCost = 0;
    int tempCompleted = 0;

    for (var item in rawData) {
      tempTotalKg += (item['kilogram'] as num?)?.toDouble() ?? 0.0;
      tempTotalCost += (item['total'] as num?)?.toDouble() ?? 0.0;
      if (item['status']?.toString().toLowerCase() == 'approved' || item['status']?.toString().toLowerCase() == 'completed') {
        tempCompleted++;
      }
    }

    if (mounted) {
      setState(() {
        _reportData = rawData;
        _totalKg = tempTotalKg;
        _totalCost = tempTotalCost;
        _completedCount = tempCompleted;
        _avgKg = rawData.isNotEmpty ? tempTotalKg / rawData.length : 0;
        _isLoading = false;
      });
    }
  }

  Future<void> _selectDate(BuildContext context, bool isStart) async {
    final initialDate = isStart ? _startDate : _endDate;
    final selected = await showDatePicker(
      context: context,
      initialDate: initialDate,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
      builder: (context, child) {
        final isDark = Theme.of(context).brightness == Brightness.dark;
        return Theme(
          data: isDark
              ? ThemeData.dark().copyWith(
                  colorScheme: const ColorScheme.dark(
                    primary: AppColors.primary,
                    onPrimary: Colors.white,
                    surface: Color(0xFF1E1E1E),
                    onSurface: Colors.white,
                  ),
                )
              : ThemeData.light().copyWith(
                  colorScheme: const ColorScheme.light(
                    primary: AppColors.primary,
                    onPrimary: Colors.white,
                    surface: Colors.white,
                    onSurface: AppColors.textPrimary,
                  ),
                ),
          child: child!,
        );
      },
    );

    if (selected != null) {
      setState(() {
        if (isStart) {
          _startDate = selected;
        } else {
          _endDate = selected;
        }
      });
      _fetchReport();
    }
  }

  void _exportReportPdf() async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser;
    if (user == null) return;

    final historyList = _reportData.map((item) {
      return SubmissionModel(
        id: 0,
        userId: user.id,
        role: user.role,
        weredaId: 0,
        weredaName: 'Wereda Info',
        mahberatId: 0,
        mahberatName: 'Assigned Entity',
        kilogram: (item['kilogram'] as num?)?.toDouble() ?? 0.0,
        rate: 0.0,
        total: (item['total'] as num?)?.toDouble() ?? 0.0,
        date: item['date']?.toString().split('T')[0] ?? '',
        time: '',
        status: item['status'] ?? 'Pending',
        notes: '',
      );
    }).toList();

    final pdfService = PdfService();
    await pdfService.generateAndPrintHistoryPdf(historyList, user.name);
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final cardColor  = isDark ? Colors.black.withValues(alpha: 0.4) : Colors.white.withValues(alpha: 0.7);
    final shadowColor = isDark ? Colors.black26 : AppColors.shadow;

    return Scaffold(
      backgroundColor: Colors.transparent,
      appBar: AppBar(
        title: const Text('Waste Analytics Report', style: TextStyle(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.transparent,
        elevation: 0,
        actions: [
          IconButton(
            icon: const Icon(Icons.picture_as_pdf, color: AppColors.primary),
            onPressed: _reportData.isEmpty ? null : _exportReportPdf,
            tooltip: 'Export PDF',
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 16.0, vertical: 8.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Date Range Filter Card
                  Container(
                    margin: const EdgeInsets.only(bottom: 16.0),
                    padding: const EdgeInsets.all(16.0),
                    decoration: BoxDecoration(
                      color: cardColor,
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: isDark ? Colors.white10 : Colors.white.withValues(alpha: 0.5)),
                      boxShadow: [BoxShadow(color: shadowColor, blurRadius: 10, offset: const Offset(0, 4))],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Filter Report Range',
                          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                        ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton.icon(
                                onPressed: () => _selectDate(context, true),
                                icon: const Icon(Icons.date_range, size: 16),
                                label: Text(DateFormat('MMM dd, yyyy').format(_startDate)),
                                style: OutlinedButton.styleFrom(
                                  padding: const EdgeInsets.symmetric(vertical: 12),
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                                ),
                              ),
                            ),
                            const Padding(
                              padding: EdgeInsets.symmetric(horizontal: 8.0),
                              child: Text('to'),
                            ),
                            Expanded(
                              child: OutlinedButton.icon(
                                onPressed: () => _selectDate(context, false),
                                icon: const Icon(Icons.date_range, size: 16),
                                label: Text(DateFormat('MMM dd, yyyy').format(_endDate)),
                                style: OutlinedButton.styleFrom(
                                  padding: const EdgeInsets.symmetric(vertical: 12),
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ).animate().fadeIn(duration: 300.ms).slideY(begin: -0.1),

                  // Metrics Cards
                  GridView.count(
                    crossAxisCount: 2,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    crossAxisSpacing: 16,
                    mainAxisSpacing: 16,
                    childAspectRatio: 1.3,
                    children: [
                      _buildMetricCard('Total Weight', '${_totalKg.toStringAsFixed(1)} kg', Icons.scale, Colors.blue, isDark),
                      _buildMetricCard('Total Valuation', '\$${_totalCost.toStringAsFixed(1)}', Icons.payments, Colors.green, isDark),
                      _buildMetricCard('Average Pickup', '${_avgKg.toStringAsFixed(1)} kg', Icons.assessment, Colors.orange, isDark),
                      _buildMetricCard('Completed Jobs', '$_completedCount', Icons.verified, Colors.purple, isDark),
                    ],
                  ).animate().fadeIn(delay: 150.ms).slideY(begin: 0.1),

                  const SizedBox(height: 24),

                  // Charts Heading
                  const Text(
                    'Weight Collection Trends',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ).animate().fadeIn(delay: 250.ms),

                  const SizedBox(height: 12),

                  // Chart Card
                  Container(
                    height: 260,
                    padding: const EdgeInsets.only(top: 24, right: 16, bottom: 8, left: 8),
                    decoration: BoxDecoration(
                      color: cardColor,
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: isDark ? Colors.white10 : Colors.white.withValues(alpha: 0.5)),
                      boxShadow: [BoxShadow(color: shadowColor, blurRadius: 10)],
                    ),
                    child: _reportData.isEmpty
                        ? const Center(child: Text('No data for selected range'))
                        : BarChart(
                            BarChartData(
                              alignment: BarChartAlignment.spaceAround,
                              maxY: _getMaxY(),
                              barTouchData: BarTouchData(
                                touchTooltipData: BarTouchTooltipData(
                                  getTooltipColor: (group) => isDark ? const Color(0xFF333333) : Colors.blueGrey,
                                  getTooltipItem: (group, groupIndex, rod, rodIndex) {
                                    return BarTooltipItem(
                                      '${rod.toY.toStringAsFixed(1)} kg',
                                      const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                                    );
                                  },
                                ),
                              ),
                              gridData: FlGridData(
                                show: true,
                                drawVerticalLine: false,
                                getDrawingHorizontalLine: (value) => FlLine(
                                  color: isDark ? Colors.white10 : Colors.black12,
                                  strokeWidth: 1,
                                ),
                              ),
                              titlesData: FlTitlesData(
                                show: true,
                                topTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                                rightTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
                                leftTitles: AxisTitles(
                                  sideTitles: SideTitles(
                                    showTitles: true,
                                    reservedSize: 40,
                                    getTitlesWidget: (value, meta) => Text(
                                      '${value.toInt()}',
                                      style: TextStyle(color: isDark ? Colors.grey[400] : Colors.grey[600], fontSize: 10),
                                    ),
                                  ),
                                ),
                                bottomTitles: AxisTitles(
                                  sideTitles: SideTitles(
                                    showTitles: true,
                                    getTitlesWidget: (value, meta) {
                                      int idx = value.toInt();
                                      if (idx >= 0 && idx < _reportData.length) {
                                        try {
                                          final fullDate = _reportData[idx]['date']?.toString() ?? '';
                                          final dt = DateTime.parse(fullDate);
                                          return Padding(
                                            padding: const EdgeInsets.only(top: 4.0),
                                            child: Text(
                                              DateFormat('MM/dd').format(dt),
                                              style: TextStyle(color: isDark ? Colors.grey[400] : Colors.grey[600], fontSize: 9, fontWeight: FontWeight.w500),
                                            ),
                                          );
                                        } catch (_) {}
                                      }
                                      return const Text('');
                                    },
                                  ),
                                ),
                              ),
                              borderData: FlBorderData(show: false),
                              barGroups: _getBarGroups(),
                            ),
                          ),
                  ).animate().fadeIn(delay: 300.ms).scale(curve: Curves.easeOutBack),

                  const SizedBox(height: 24),

                  // Data Summary Table Heading
                  if (_reportData.isNotEmpty) ...[
                    const Text(
                      'Report Details Table',
                      style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                    ).animate().fadeIn(delay: 350.ms),
                    const SizedBox(height: 12),
                    Card(
                      elevation: 1,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                      color: isDark ? const Color(0xFF1E1E1E) : Colors.white,
                      child: Padding(
                        padding: const EdgeInsets.all(8.0),
                        child: DataTable(
                          columnSpacing: 16.0,
                          horizontalMargin: 8.0,
                          columns: const [
                            DataColumn(label: Text('Date', style: TextStyle(fontWeight: FontWeight.bold))),
                            DataColumn(label: Text('KG', style: TextStyle(fontWeight: FontWeight.bold))),
                            DataColumn(label: Text('Valuation', style: TextStyle(fontWeight: FontWeight.bold))),
                            DataColumn(label: Text('Status', style: TextStyle(fontWeight: FontWeight.bold))),
                          ],
                          rows: _reportData.map((item) {
                            String dateStr = '';
                            try {
                              dateStr = DateFormat('MMM dd').format(DateTime.parse(item['date']?.toString() ?? ''));
                            } catch (_) {
                              dateStr = item['date']?.toString().split('T')[0] ?? '';
                            }
                            return DataRow(cells: [
          DataCell(Text(dateStr)),
                              DataCell(Text(((item['kilogram'] as num?)?.toDouble().toStringAsFixed(1) ?? '0'))),
                              DataCell(Text('ETB ${(item['total'] as num?)?.toDouble().toStringAsFixed(1) ?? '0'}')),
                              DataCell(Container(
                                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                decoration: BoxDecoration(
                                  color: (item['status']?.toString().toLowerCase() == 'approved' ? Colors.green : Colors.orange).withValues(alpha: 0.1),
                                  borderRadius: BorderRadius.circular(6),
                                  border: Border.all(color: item['status']?.toString().toLowerCase() == 'approved' ? Colors.green : Colors.orange),
                                ),
                                child: Text(item['status']?.toString() ?? 'Pending', style: TextStyle(fontSize: 10, color: item['status']?.toString().toLowerCase() == 'approved' ? Colors.green : Colors.orange, fontWeight: FontWeight.bold)),
                              )),
                            ]);
                          }).toList(),
                        ),
                      ),
                    ).animate().fadeIn(delay: 400.ms),
                    const SizedBox(height: 80), // bottom nav padding
                  ],
                ],
              ),
            ),
    );
  }

  Widget _buildMetricCard(String title, String value, IconData icon, Color color, bool isDark) {
    final boxColor   = isDark ? Colors.black.withValues(alpha: 0.3) : Colors.white.withValues(alpha: 0.8);
    final labelColor = isDark ? Colors.grey[400] : AppColors.textSecondary;
    final valColor   = isDark ? Colors.white : AppColors.textPrimary;

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: boxColor,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: isDark ? Colors.white10 : Colors.white60),
        boxShadow: [
          BoxShadow(
            color: isDark ? Colors.black12 : AppColors.shadow,
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                title,
                style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: labelColor),
              ),
              Icon(icon, color: color, size: 20),
            ],
          ),
          const Spacer(),
          Text(
            value,
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: valColor),
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }

  double _getMaxY() {
    double maxKg = 10;
    for (var item in _reportData) {
      double kg = (item['kilogram'] as num?)?.toDouble() ?? 0.0;
      if (kg > maxKg) maxKg = kg;
    }
    return maxKg * 1.15;
  }

  List<BarChartGroupData> _getBarGroups() {
    List<BarChartGroupData> groups = [];
    for (int i = 0; i < _reportData.length; i++) {
      double kg = (_reportData[i]['kilogram'] as num?)?.toDouble() ?? 0.0;
      groups.add(
        BarChartGroupData(
          x: i,
          barRods: [
            BarChartRodData(
              toY: kg,
              gradient: const LinearGradient(
                colors: [AppColors.primary, AppColors.secondary],
                begin: Alignment.bottomCenter,
                end: Alignment.topCenter,
              ),
              width: 14,
              borderRadius: const BorderRadius.only(
                topLeft: Radius.circular(6),
                topRight: Radius.circular(6),
              ),
            ),
          ],
        ),
      );
    }
    return groups;
  }
}
