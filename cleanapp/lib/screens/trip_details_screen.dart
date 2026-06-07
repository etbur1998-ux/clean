import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import '../models/transport_request_model.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../theme/app_colors.dart';

class TripDetailsScreen extends StatefulWidget {
  final int requestId;
  final String requestNumber;

  const TripDetailsScreen({
    super.key,
    required this.requestId,
    required this.requestNumber,
  });

  @override
  State<TripDetailsScreen> createState() => _TripDetailsScreenState();
}

class _TripDetailsScreenState extends State<TripDetailsScreen> {
  TransportRequestModel? _request;
  bool _isLoading = true;
  bool _isProcessing = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadTripDetails();
  }

  Future<void> _loadTripDetails() async {
    setState(() { _isLoading = true; _error = null; });
    try {
      final detail = await _fetchTransportDetail(widget.requestId);
      if (detail != null) {
        setState(() { _request = detail; _isLoading = false; });
      } else {
        setState(() { _error = 'Failed to load trip details'; _isLoading = false; });
      }
    } catch (e) {
      setState(() { _error = 'Error: $e'; _isLoading = false; });
    }
  }

  Future<TransportRequestModel?> _fetchTransportDetail(int id) async {
    try {
      final url = '${ApiService.transportApiBaseUrl}/requests/$id';
      final response = await http.get(Uri.parse(url));
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        return TransportRequestModel.fromJson(data['request'] ?? data);
      }
      return null;
    } catch (_) {
      return null;
    }
  }

  // ── Snackbar helper ──────────────────────────────────────────────────────
  void _snack(String msg, {Color? color}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(msg),
      backgroundColor: color,
      behavior: SnackBarBehavior.floating,
    ));
  }

  // ── ACCEPT ───────────────────────────────────────────────────────────────
  Future<void> _acceptTrip() async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser;
    if (user == null) return;
    setState(() => _isProcessing = true);
    try {
      final ok = await ApiService().acceptTransportRequest(
        widget.requestId,
        driverId: user.id,
        driverName: user.name,
        notes: 'Accepted via mobile app',
      );
      if (!mounted) return;
      if (ok) {
        _snack('Trip accepted!', color: Colors.green);
        await _loadTripDetails();
      } else {
        _snack('Failed to accept trip', color: Colors.red);
      }
    } catch (e) {
      if (mounted) _snack('Error: $e');
    } finally {
      if (mounted) setState(() => _isProcessing = false);
    }
  }

  // ── REJECT ───────────────────────────────────────────────────────────────
  Future<void> _rejectTrip() async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser;
    if (user == null) return;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Reject Trip?'),
        content: const Text('Are you sure you want to reject this trip?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Reject', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    setState(() => _isProcessing = true);
    try {
      final ok = await ApiService().rejectTransportRequest(
        widget.requestId,
        driverId: user.id,
        driverName: user.name,
        notes: 'Rejected via mobile app',
      );
      if (!mounted) return;
      if (ok) {
        _snack('Trip rejected', color: Colors.orange);
        Navigator.pop(context, true);
      } else {
        _snack('Failed to reject trip', color: Colors.red);
      }
    } catch (e) {
      if (mounted) _snack('Error: $e');
    } finally {
      if (mounted) setState(() => _isProcessing = false);
    }
  }

  // ── CONFIRM PICKUP ───────────────────────────────────────────────────────
  Future<void> _confirmPickup() async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser;
    if (user == null) return;
    setState(() => _isProcessing = true);
    try {
      final ok = await ApiService().markTransportPickedUp(
        widget.requestId,
        driverId: user.id,
        driverName: user.name,
        notes: 'Payload loaded',
      );
      if (!mounted) return;
      if (ok) {
        _snack('Pickup confirmed!', color: Colors.blue);
        await _loadTripDetails();
      } else {
        _snack('Failed to confirm pickup', color: Colors.red);
      }
    } catch (e) {
      if (mounted) _snack('Error: $e');
    } finally {
      if (mounted) setState(() => _isProcessing = false);
    }
  }

  // ── SUBMIT RECEIPT ───────────────────────────────────────────────────────
  Future<void> _submitReceipt() async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser;
    if (user == null) return;

    // Collect receipt data via bottom sheet
    final result = await showModalBottomSheet<Map<String, dynamic>>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _ReceiptSheet(requestId: widget.requestId),
    );
    if (result == null || !mounted) return;

    setState(() => _isProcessing = true);
    try {
      // Upload photo if provided
      String? photoUrl;
      if (result['image'] != null) {
        photoUrl = await ApiService().uploadImage(result['image']);
      }
      final ok = await ApiService().submitTransportReceipt(
        widget.requestId,
        driverId: user.id,
        driverName: user.name,
        receiptPhotoUrl: photoUrl,
        notes: result['notes'] ?? '',
        actualKilogram: result['kilogram'],
        weredaId: result['weredaId'],
        mahberatId: result['mahberatId'],
      );
      if (!mounted) return;
      if (ok) {
        _snack('Receipt submitted!', color: Colors.green);
        await _loadTripDetails();
      } else {
        _snack('Failed to submit receipt', color: Colors.red);
      }
    } catch (e) {
      if (mounted) _snack('Error: $e');
    } finally {
      if (mounted) setState(() => _isProcessing = false);
    }
  }

  // ── STATUS STEP TRACKER ──────────────────────────────────────────────────
  Widget _buildStepTracker(String status) {
    final steps = ['Assigned', 'Accepted', 'Picked Up', 'Receipt', 'Paid'];
    final rejected = ['DispatcherRejected', 'DriverRejected', 'StaffRejected'];
    if (rejected.contains(status)) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        decoration: BoxDecoration(
          color: Colors.red[50],
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: Colors.red[200]!),
        ),
        child: Row(children: [
          const Icon(Icons.cancel_outlined, color: Colors.red, size: 16),
          const SizedBox(width: 6),
          Text('Rejected', style: TextStyle(
              color: Colors.red[700], fontWeight: FontWeight.bold, fontSize: 12)),
        ]),
      );
    }
    final idx = const {
      'DriverAssigned': 0, 'DriverAccepted': 1, 'PickedUp': 2,
      'MahberatApprovedPickup': 2, 'ReceiptSubmitted': 3,
      'ReceiptVerified': 3, 'StaffApproved': 4, 'Paid': 4,
    }[status] ?? 0;

    return Row(
      children: List.generate(steps.length * 2 - 1, (i) {
        if (i.isOdd) {
          return Expanded(child: Container(
            height: 3,
            color: (i ~/ 2) < idx ? AppColors.primary : Colors.grey[300],
          ));
        }
        final si = i ~/ 2;
        final done = si < idx;
        final active = si == idx;
        return Column(children: [
          Container(
            width: 26, height: 26,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: done ? AppColors.primary : active ? Colors.white : Colors.grey[200],
              border: Border.all(
                color: done || active ? AppColors.primary : Colors.grey[300]!,
                width: active ? 2.5 : 1.5,
              ),
            ),
            child: Center(child: done
                ? const Icon(Icons.check, size: 13, color: Colors.white)
                : Text('${si + 1}', style: TextStyle(
                    fontSize: 10, fontWeight: FontWeight.bold,
                    color: active ? AppColors.primary : Colors.grey[500]))),
          ),
          const SizedBox(height: 3),
          Text(steps[si], style: TextStyle(
            fontSize: 8,
            color: done || active ? AppColors.primary : Colors.grey[400],
            fontWeight: active ? FontWeight.bold : FontWeight.normal,
          )),
        ]);
      }),
    );
  }

  // ── ACTION BUTTONS based on current status ───────────────────────────────
  Widget _buildActions(String status) {
    if (_isProcessing) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 16),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    switch (status) {
      case 'DriverAssigned':
        return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          ElevatedButton.icon(
            onPressed: _acceptTrip,
            icon: const Icon(Icons.check_circle),
            label: const Text('Accept Trip'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.green, foregroundColor: Colors.white,
              minimumSize: const Size.fromHeight(50),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
          ),
          const SizedBox(height: 12),
          OutlinedButton.icon(
            onPressed: _rejectTrip,
            icon: const Icon(Icons.close),
            label: const Text('Reject Trip'),
            style: OutlinedButton.styleFrom(
              foregroundColor: Colors.red,
              side: const BorderSide(color: Colors.red),
              minimumSize: const Size.fromHeight(50),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
          ),
        ]);

      case 'DriverAccepted':
        return ElevatedButton.icon(
          onPressed: _confirmPickup,
          icon: const Icon(Icons.hail_rounded),
          label: const Text('Confirm Payload Loaded'),
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary, foregroundColor: Colors.white,
            minimumSize: const Size.fromHeight(50),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
        );

      case 'PickedUp':
      case 'MahberatApprovedPickup':
        return ElevatedButton.icon(
          onPressed: _submitReceipt,
          icon: const Icon(Icons.receipt_long_rounded),
          label: const Text('Submit Receipt (KG + Photo)'),
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.secondary, foregroundColor: Colors.white,
            minimumSize: const Size.fromHeight(50),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
        );

      default:
        // Terminal / waiting statuses
        final info = const {
          'ReceiptSubmitted': ('Receipt submitted — awaiting review.', Colors.orange),
          'ReceiptVerified':  ('Receipt verified — awaiting staff approval.', Colors.purple),
          'StaffApproved':    ('Approved — payment being processed.', Colors.deepPurple),
          'Paid':             ('Payment complete. Job done!', Colors.green),
          'DispatcherRejected': ('This trip was rejected by dispatcher.', Colors.red),
          'DriverRejected':     ('You rejected this trip.', Colors.red),
          'StaffRejected':      ('This trip was rejected by staff.', Colors.red),
        }[status];
        final color = info?.$2 ?? Colors.grey;
        final msg   = info?.$1 ?? 'Awaiting next action.';
        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.08),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: color.withValues(alpha: 0.3)),
          ),
          child: Row(children: [
            Icon(
              status == 'Paid' ? Icons.check_circle_outline : Icons.hourglass_bottom_rounded,
              color: color, size: 20,
            ),
            const SizedBox(width: 10),
            Expanded(child: Text(msg,
                style: TextStyle(color: color, fontWeight: FontWeight.w600, fontSize: 13))),
          ]),
        );
    }
  }

  // ══════════════════════════════════════════════════════════════════════════
  // BUILD
  // ══════════════════════════════════════════════════════════════════════════

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: Text('Trip ${widget.requestNumber}'),
        elevation: 1,
        backgroundColor: AppColors.surface,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadTripDetails,
            tooltip: 'Refresh',
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.error_outline, size: 64, color: Colors.red[400]),
                    const SizedBox(height: 16),
                    Text(_error!),
                    const SizedBox(height: 16),
                    ElevatedButton(onPressed: _loadTripDetails, child: const Text('Retry')),
                  ],
                ))
              : _request == null
                  ? const Center(child: Text('No trip data'))
                  : SingleChildScrollView(
                      padding: const EdgeInsets.all(16),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Status badge
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                            decoration: BoxDecoration(
                              color: _statusColor(_request!.status).withValues(alpha: 0.15),
                              borderRadius: BorderRadius.circular(20),
                            ),
                            child: Text(
                              _request!.getStatusDisplay(),
                              style: TextStyle(
                                color: _statusColor(_request!.status),
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                          const SizedBox(height: 16),

                          // Step tracker
                          _buildStepTracker(_request!.status),
                          const SizedBox(height: 20),

                          // Request number
                          Text('Request ID',
                              style: Theme.of(context).textTheme.labelMedium
                                  ?.copyWith(color: Colors.grey[600])),
                          Text(_request!.requestNumber,
                              style: Theme.of(context).textTheme.headlineSmall
                                  ?.copyWith(fontWeight: FontWeight.bold)),
                          const SizedBox(height: 20),

                          _infoTile('Pickup Location', _request!.pickupLocation, Icons.location_on, isDark),
                          const SizedBox(height: 10),
                          _infoTile('Destination', _request!.destination, Icons.flag_rounded, isDark),
                          const SizedBox(height: 10),
                          _infoTile('Cargo / Passengers', _request!.passengerItemDetails, Icons.backpack_rounded, isDark),
                          const SizedBox(height: 10),
                          Row(children: [
                            Expanded(child: _infoTile('Date', _request!.requestedDate, Icons.calendar_today, isDark)),
                            const SizedBox(width: 10),
                            Expanded(child: _infoTile('Time', _request!.requestedTime, Icons.access_time, isDark)),
                          ]),
                          if ((_request!.specialInstructions ?? '').isNotEmpty) ...[
                            const SizedBox(height: 10),
                            _infoTile('Instructions', _request!.specialInstructions!, Icons.info_outline, isDark),
                          ],
                          if (_request!.vehiclePlate != null) ...[
                            const SizedBox(height: 10),
                            _infoTile('Assigned Vehicle', _request!.vehiclePlate!, Icons.local_shipping, isDark),
                          ],
                          if (_request!.driverName != null) ...[
                            const SizedBox(height: 10),
                            _infoTile('Driver', _request!.driverName!, Icons.person_outline, isDark),
                          ],
                          if (_request!.transportCost != null) ...[
                            const SizedBox(height: 10),
                            _infoTile('Transport Cost', '${_request!.transportCost!.toStringAsFixed(2)} ETB',
                                Icons.attach_money_rounded, isDark),
                          ],
                          const SizedBox(height: 28),

                          // Action buttons
                          _buildActions(_request!.status),
                          const SizedBox(height: 24),
                        ],
                      ),
                    ),
    );
  }

  Widget _infoTile(String label, String value, IconData icon, bool isDark) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: isDark ? const Color(0xFF1E2D2C) : Colors.grey[100],
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Icon(icon, size: 18, color: AppColors.primary),
        const SizedBox(width: 10),
        Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(label, style: TextStyle(fontSize: 11,
              color: isDark ? Colors.white54 : Colors.grey[600],
              fontWeight: FontWeight.w500)),
          const SizedBox(height: 2),
          Text(value, style: TextStyle(
              fontSize: 14, fontWeight: FontWeight.bold,
              color: isDark ? Colors.white : Colors.black87)),
        ])),
      ]),
    );
  }

  Color _statusColor(String status) {
    switch (status) {
      case 'DriverAssigned':         return Colors.blue;
      case 'DriverAccepted':         return Colors.green;
      case 'PickedUp':               return Colors.indigo;
      case 'MahberatApprovedPickup': return Colors.teal;
      case 'ReceiptSubmitted':       return Colors.orange;
      case 'ReceiptVerified':        return Colors.purple;
      case 'StaffApproved':          return Colors.deepPurple;
      case 'Paid':                   return Colors.green;
      case 'DispatcherRejected':
      case 'DriverRejected':
      case 'StaffRejected':          return Colors.red;
      default:                       return Colors.grey;
    }
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// RECEIPT BOTTOM SHEET
// ══════════════════════════════════════════════════════════════════════════════

class _ReceiptSheet extends StatefulWidget {
  final int requestId;
  const _ReceiptSheet({required this.requestId});
  @override
  State<_ReceiptSheet> createState() => _ReceiptSheetState();
}

class _ReceiptSheetState extends State<_ReceiptSheet> {
  final _kgCtrl    = TextEditingController();
  final _notesCtrl = TextEditingController();
  XFile? _image;
  List<Map<String, dynamic>> _weredas    = [];
  List<Map<String, dynamic>> _mahberats  = [];
  int? _selWereda;
  int? _selMahberat;
  bool _loadingDropdowns = true;

  @override
  void initState() {
    super.initState();
    _loadDropdowns();
  }

  @override
  void dispose() {
    _kgCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadDropdowns() async {
    final results = await Future.wait([
      ApiService().getWeredas(),
      ApiService().getMahberats(),
    ]);
    if (mounted) {
      setState(() {
        _weredas   = results[0];
        _mahberats = results[1];
        _loadingDropdowns = false;
      });
    }
  }

  Future<void> _pickImage() async {
    final picked = await ImagePicker().pickImage(source: ImageSource.camera);
    if (picked != null) setState(() => _image = picked);
  }

  void _submit() {
    final kg = double.tryParse(_kgCtrl.text.trim());
    if (kg == null || kg <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Enter a valid KG amount')));
      return;
    }
    Navigator.pop(context, {
      'kilogram':   kg,
      'notes':      _notesCtrl.text.trim(),
      'image':      _image,
      'weredaId':   _selWereda,
      'mahberatId': _selMahberat,
    });
  }

  InputDecoration _dec(String label, IconData icon) => InputDecoration(
    labelText: label,
    prefixIcon: Icon(icon, size: 20, color: AppColors.primary),
    filled: true,
    fillColor: Colors.grey[100],
    border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12), borderSide: BorderSide.none),
    focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.primary, width: 2)),
    contentPadding: const EdgeInsets.symmetric(vertical: 14, horizontal: 12),
  );

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      padding: EdgeInsets.fromLTRB(
          20, 20, 20, MediaQuery.of(context).viewInsets.bottom + 24),
      child: SingleChildScrollView(
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          // Handle
          Center(child: Container(
            width: 40, height: 4,
            decoration: BoxDecoration(
                color: Colors.grey[300], borderRadius: BorderRadius.circular(4)),
          )),
          const SizedBox(height: 16),
          const Text('Submit Receipt',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
          const SizedBox(height: 4),
          Text('Fill in the collection details for this trip',
              style: TextStyle(fontSize: 13, color: Colors.grey[600])),
          const SizedBox(height: 20),

          if (_loadingDropdowns)
            const Center(child: CircularProgressIndicator())
          else ...[
            // Wereda
            DropdownButtonFormField<int>(
              decoration: _dec('Wereda', Icons.location_on),
              value: _selWereda,
              items: _weredas.map((w) => DropdownMenuItem<int>(
                  value: w['id'] as int, child: Text(w['name'] as String))).toList(),
              onChanged: (v) => setState(() { _selWereda = v; _selMahberat = null; }),
            ),
            const SizedBox(height: 14),
            // Mahberat
            DropdownButtonFormField<int>(
              decoration: _dec('Mahberat', Icons.business),
              value: _selMahberat,
              items: _mahberats.map((m) => DropdownMenuItem<int>(
                  value: m['id'] as int, child: Text(m['name'] as String))).toList(),
              onChanged: (v) => setState(() => _selMahberat = v),
            ),
            const SizedBox(height: 14),
            // KG
            TextField(
              controller: _kgCtrl,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: _dec('Kilograms collected', Icons.scale),
            ),
            const SizedBox(height: 14),
            // Notes
            TextField(
              controller: _notesCtrl,
              maxLines: 2,
              decoration: _dec('Notes (optional)', Icons.notes),
            ),
            const SizedBox(height: 14),
            // Photo
            GestureDetector(
              onTap: _pickImage,
              child: Container(
                height: 80,
                decoration: BoxDecoration(
                  color: Colors.grey[100],
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.grey[300]!),
                ),
                child: _image == null
                    ? Column(mainAxisAlignment: MainAxisAlignment.center, children: [
                        Icon(Icons.camera_alt_outlined,
                            size: 28, color: Colors.grey[500]),
                        const SizedBox(height: 4),
                        Text('Tap to take receipt photo',
                            style: TextStyle(fontSize: 12, color: Colors.grey[500])),
                      ])
                    : Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                        const Icon(Icons.check_circle, color: Colors.green, size: 20),
                        const SizedBox(width: 8),
                        Text('Photo captured', style: TextStyle(
                            color: Colors.green[700], fontWeight: FontWeight.w600)),
                      ]),
              ),
            ),
            const SizedBox(height: 20),
            ElevatedButton.icon(
              onPressed: _submit,
              icon: const Icon(Icons.upload_rounded),
              label: const Text('Submit Receipt'),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary, foregroundColor: Colors.white,
                minimumSize: const Size.fromHeight(50),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
            ),
          ],
        ]),
      ),
    );
  }
}
