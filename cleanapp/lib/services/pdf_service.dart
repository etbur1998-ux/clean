import 'dart:io';
import 'package:flutter/services.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';
import 'package:path_provider/path_provider.dart';
import '../models/submission_model.dart';

class PdfService {
  Future<void> generateAndPrintHistoryPdf(List<SubmissionModel> history, String userName) async {
    final pdf = pw.Document();

    pdf.addPage(
      pw.MultiPage(
        pageFormat: PdfPageFormat.a4,
        margin: const pw.EdgeInsets.all(32),
        build: (pw.Context context) {
          return [
            pw.Header(
              level: 0,
              child: pw.Row(
                mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
                children: [
                  pw.Text('Work History Report', style: pw.TextStyle(fontSize: 24, fontWeight: pw.FontWeight.bold)),
                  pw.Text('Date: ${DateTime.now().toString().split(' ')[0]}'),
                ]
              )
            ),
            pw.SizedBox(height: 20),
            pw.Text('Prepared by: $userName', style: pw.TextStyle(fontSize: 16)),
            pw.SizedBox(height: 20),
            _buildTable(history),
            pw.SizedBox(height: 30),
            _buildSummary(history),
          ];
        },
      ),
    );

    await Printing.layoutPdf(onLayout: (PdfPageFormat format) async => pdf.save());
  }

  pw.Widget _buildTable(List<SubmissionModel> history) {
    return pw.Table.fromTextArray(
      headers: ['Date', 'Location', 'KG', 'Rate', 'Total', 'Status'],
      data: history.map((item) => [
        item.date,
        item.weredaName ?? 'Unknown',
        item.kilogram.toStringAsFixed(1),
        '\$${item.rate.toStringAsFixed(2)}',
        '\$${item.total.toStringAsFixed(2)}',
        item.status,
      ]).toList(),
      headerStyle: pw.TextStyle(fontWeight: pw.FontWeight.bold, color: PdfColors.white),
      headerDecoration: const pw.BoxDecoration(color: PdfColors.blueGrey800),
      rowDecoration: const pw.BoxDecoration(border: pw.Border(bottom: pw.BorderSide(color: PdfColors.grey300))),
      cellAlignment: pw.Alignment.centerLeft,
    );
  }

  pw.Widget _buildSummary(List<SubmissionModel> history) {
    double totalKg = 0;
    double totalAmount = 0;
    for (var item in history) {
      totalKg += item.kilogram;
      totalAmount += item.total;
    }

    return pw.Container(
      padding: const pw.EdgeInsets.all(10),
      decoration: pw.BoxDecoration(
        color: PdfColors.grey100,
        borderRadius: const pw.BorderRadius.all(pw.Radius.circular(8)),
      ),
      child: pw.Column(
        crossAxisAlignment: pw.CrossAxisAlignment.start,
        children: [
          pw.Text('Summary', style: pw.TextStyle(fontSize: 18, fontWeight: pw.FontWeight.bold)),
          pw.SizedBox(height: 10),
          pw.Text('Total Jobs: ${history.length}'),
          pw.Text('Total Kilograms: ${totalKg.toStringAsFixed(1)} kg'),
          pw.Text('Total Amount: \$${totalAmount.toStringAsFixed(2)}', style: pw.TextStyle(fontWeight: pw.FontWeight.bold)),
        ]
      )
    );
  }
}
