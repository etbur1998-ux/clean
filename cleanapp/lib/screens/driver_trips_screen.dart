import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';
import 'package:geolocator/geolocator.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import '../services/auth_service.dart';
import '../services/api_service.dart';
import '../services/local_db_service.dart';
import '../models/submission_model.dart';
import '../theme/app_colors.dart';
import 'package:go_router/go_router.dart';

class DriverTripsScreen extends StatefulWidget {
  const DriverTripsScreen({super.key});
  @override
  State<DriverTripsScreen> createState() => _DriverTripsScreenState();
}

class _DriverTripsScreenState extends State<DriverTripsScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final ApiService _apiService = ApiService();
  bool _isLoading = false;

  // â”€â”€ Transport trips â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  List<Map<String, dynamic>> _transportTrips = [];
  String _filterStatus = 'all'; // all | active | done

  // â”€â”€ Receipt submission â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  final _formKey = GlobalKey<FormState>();
  List<Map<String, dynamic>> _weredas = [];
  List<Map<String, dynamic>> _mahberats = [];
  List<Map<String, dynamic>> _companies = [];
  List<Map<String, dynamic>> _vehicles = [];
  String _receiptType = 'Mahberat';
  int? _selWereda;
  int? _selEntity;
  int? _selVehicle;
  final _kgCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  List<XFile> _images = [];
  bool _submitting = false;

  // Mock data for offline testing
  final List<Map<String, dynamic>> _mockTransport = [
    {
      'id': 201,
      'request_number': 'TR-20260603-99V5',
      'pickup_location': 'Yeka Subcity Store A',
      'destination': 'Gotera Warehouse 3',
      'passenger_item_details': '50 packages cleaning detergents',
      'requested_date': '2026-06-03',
      'requested_time': '09:00',
      'status': 'DriverAssigned',
      'mahberat_name': 'Yeka Logistic Corp',
      'special_instructions': 'Verify seals before loading.',
    },
    {
      'id': 202,
      'request_number': 'TR-20260603-FF22',
      'pickup_location': 'Gerji Condominium Gate 2',
      'destination': 'Cleaning Plant 1',
      'passenger_item_details': '3 municipal sweepers staff',
      'requested_date': '2026-06-03',
      'requested_time': '11:15',
      'status': 'PickedUp',
      'mahberat_name': 'Gerji Cleaners Union',
      'special_instructions': 'Passenger transport. Drive carefully.',
    },
  ];

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _loadTrips();
    _loadDropdowns();
  }

  @override
  void dispose() {
    _tabController.dispose();
    _kgCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
  // TRANSPORT LOAD
  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

  Future<void> _loadTrips() async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser;
    if (user == null) return;
    setState(() => _isLoading = true);
    try {
      final trips = await _apiService.getDriverTransportRequests(user.id);
      setState(() {
        _transportTrips = trips.isNotEmpty ? trips : _mockTransport;
        _isLoading = false;
      });
    } catch (_) {
      setState(() {
        _transportTrips = _mockTransport;
        _isLoading = false;
      });
    }
  }

  List<Map<String, dynamic>> get _filteredTrips {
    switch (_filterStatus) {
      case 'active':
        return _transportTrips.where((t) {
          final s = t['status'] ?? '';
          return [
            'DriverAssigned',
            'DriverAccepted',
            'PickedUp',
            'MahberatApprovedPickup',
          ].contains(s);
        }).toList();
      case 'done':
        return _transportTrips.where((t) {
          final s = t['status'] ?? '';
          return [
            'ReceiptSubmitted',
            'ReceiptVerified',
            'StaffApproved',
            'Paid',
            'DispatcherRejected',
            'DriverRejected',
            'StaffRejected',
          ].contains(s);
        }).toList();
      default:
        return _transportTrips;
    }
  }

  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
  // RECEIPT SUBMISSION LOAD
  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

  Future<void> _loadDropdowns() async {
    final results = await Future.wait([
      _apiService.getWeredas(),
      _apiService.getMahberats(),
      _apiService.getCompanies(),
      _apiService.getVehicles(),
    ]);
    if (mounted) {
      setState(() {
        _weredas = results[0];
        _mahberats = results[1];
        _companies = results[2];
        _vehicles = results[3];
      });
    }
  }

  Future<void> _pickImages() async {
    final picked = await ImagePicker().pickMultiImage();
    if (picked.isNotEmpty) setState(() => _images.addAll(picked));
  }

  Future<void> _submitReceipt() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selWereda == null || _selEntity == null) {
      _snack(
        'Please select Wereda and ${_receiptType == 'Mahberat' ? 'Mahberat' : 'Company'}',
      );
      return;
    }
    setState(() => _submitting = true);

    double? lat, lng;
    try {
      if (await Geolocator.isLocationServiceEnabled()) {
        var perm = await Geolocator.checkPermission();
        if (perm == LocationPermission.denied)
          perm = await Geolocator.requestPermission();
        if (perm == LocationPermission.whileInUse ||
            perm == LocationPermission.always) {
          final pos = await Geolocator.getCurrentPosition();
          lat = pos.latitude;
          lng = pos.longitude;
        }
      }
    } catch (_) {}

    final user = Provider.of<AuthService>(context, listen: false).currentUser!;
    final now = DateTime.now();
    List<String> urls = [];
    for (final img in _images) {
      final u = await _apiService.uploadImage(img);
      if (u != null) urls.add(u);
    }

    final sub = SubmissionModel(
      userId: user.id,
      role: user.role,
      weredaId: _selWereda!,
      mahberatId: _selEntity!,
      vehicleId: _selVehicle ?? user.vehicleId,
      kilogram: double.tryParse(_kgCtrl.text) ?? 0.0,
      rate: 0.0,
      total: 0.0,
      date: DateFormat('yyyy-MM-dd').format(now),
      time: DateFormat('HH:mm').format(now),
      notes: _notesCtrl.text,
      imageUrl: urls.isNotEmpty ? urls.join(',') : null,
      latitude: lat,
      longitude: lng,
      receiptType: _receiptType,
    );

    final conn = await Connectivity().checkConnectivity();
    bool online = conn.isNotEmpty && conn.first != ConnectivityResult.none;
    bool ok = false, offline = false;
    if (online) {
      ok = await _apiService.submitWork(sub);
      if (!ok) {
        await LocalDbService().saveOfflineSubmission(sub);
        offline = true;
      }
    } else {
      await LocalDbService().saveOfflineSubmission(sub);
      offline = true;
    }

    if (mounted) {
      setState(() {
        _submitting = false;
        _images = [];
        _kgCtrl.clear();
        _notesCtrl.clear();
      });
      _snack(
        ok
            ? 'Receipt submitted!'
            : offline
            ? 'Saved offline â€” will sync later.'
            : 'Submission failed.',
        color: ok
            ? Colors.green
            : offline
            ? Colors.orange
            : Colors.red,
      );
    }
  }

  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
  // TRANSPORT ACTION HANDLERS
  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

  Future<void> _accept(Map<String, dynamic> trip) async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser!;
    setState(() => _isLoading = true);
    final ok = await _apiService.acceptTransportRequest(
      trip['id'],
      driverId: user.id,
      driverName: user.name,
    );
    if (ok) {
      _snack('Job Accepted', color: Colors.green);
    } else {
      trip['status'] = 'DriverAccepted';
      _snack('Simulated: Accepted (offline)');
    }
    setState(() => _isLoading = false);
    _loadTrips();
  }

  Future<void> _reject(Map<String, dynamic> trip) async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser!;
    setState(() => _isLoading = true);
    final ok = await _apiService.rejectTransportRequest(
      trip['id'],
      driverId: user.id,
      driverName: user.name,
    );
    if (ok) {
      _snack('Job Rejected', color: Colors.orange);
    } else {
      _transportTrips.removeWhere((t) => t['id'] == trip['id']);
      _snack('Simulated: Rejected (offline)');
    }
    setState(() => _isLoading = false);
    _loadTrips();
  }

  Future<void> _pickup(Map<String, dynamic> trip) async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser!;
    setState(() => _isLoading = true);
    final ok = await _apiService.markTransportPickedUp(
      trip['id'],
      driverId: user.id,
      driverName: user.name,
      notes: 'Payload loaded',
    );
    if (ok) {
      _snack('Pickup Confirmed', color: Colors.blue);
    } else {
      trip['status'] = 'PickedUp';
      _snack('Simulated: Picked up (offline)');
    }
    setState(() => _isLoading = false);
    _loadTrips();
  }

  // Show receipt form for PickedUp / MahberatApprovedPickup
  Future<void> _submitReceiptPhoto(Map<String, dynamic> trip) async {
    final user = Provider.of<AuthService>(context, listen: false).currentUser!;
    if (_weredas.isEmpty) await _loadDropdowns();
    if (!mounted) return;
    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _TransportReceiptSheet(
        trip: trip,
        user: user,
        apiService: _apiService,
        weredas: _weredas,
        mahberats: _mahberats,
        onDone: (ok) {
          _snack(
            ok ? 'Receipt Submitted!' : 'Saved offline  will sync later.',
            color: ok ? Colors.green : Colors.orange,
          );
          _loadTrips();
        },
      ),
    );
  }

  // Show read-only trip details (after receipt submitted)
  void _viewTripDetails(Map<String, dynamic> trip) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _TripDetailSheet(trip: trip),
    );
  }

  void _snack(String msg, {Color? color}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(msg),
        backgroundColor: color,
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
  // BUILD
  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final bg = isDark ? const Color(0xFF1E1E1E) : AppColors.surface;
    final fg = isDark ? Colors.white : AppColors.textPrimary;

    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Navigator.canPop(context)
              ? Navigator.pop(context)
              : context.go('/home'),
        ),
        title: const Text(
          'Driver Console',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        backgroundColor: bg,
        foregroundColor: fg,
        bottom: TabBar(
          controller: _tabController,
          labelColor: AppColors.primary,
          unselectedLabelColor: isDark ? Colors.grey[400] : Colors.grey[600],
          indicatorColor: AppColors.primary,
          tabs: const [
            Tab(
              icon: Icon(Icons.local_shipping_outlined),
              text: 'Transport Jobs',
            ),
            Tab(
              icon: Icon(Icons.receipt_long_outlined),
              text: 'Submit Receipt',
            ),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [_buildTransportTab(isDark), _buildReceiptTab(isDark)],
      ),
    );
  }

  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
  // TAB 1 â€” TRANSPORT JOBS
  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

  Widget _buildTransportTab(bool isDark) {
    return Column(
      children: [
        // â”€â”€ Filter bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        Container(
          color: isDark ? const Color(0xFF1E1E1E) : Colors.white,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          child: Row(
            children: [
              const Text(
                'Filter:',
                style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13),
              ),
              const SizedBox(width: 10),
              _filterChip('All', 'all'),
              const SizedBox(width: 8),
              _filterChip('Active', 'active'),
              const SizedBox(width: 8),
              _filterChip('Done', 'done'),
              const Spacer(),
              IconButton(
                icon: const Icon(Icons.refresh),
                onPressed: _loadTrips,
                tooltip: 'Refresh',
                color: AppColors.primary,
              ),
            ],
          ),
        ),
        const Divider(height: 1),
        // â”€â”€ List â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        Expanded(
          child: _isLoading
              ? const Center(child: CircularProgressIndicator())
              : RefreshIndicator(
                  onRefresh: _loadTrips,
                  child: _filteredTrips.isEmpty
                      ? _emptyState(
                          icon: Icons.local_shipping_outlined,
                          msg: _filterStatus == 'all'
                              ? 'No transport jobs assigned'
                              : 'No $_filterStatus jobs',
                        )
                      : ListView.builder(
                          padding: const EdgeInsets.all(16),
                          itemCount: _filteredTrips.length,
                          itemBuilder: (_, i) =>
                              _buildTripCard(_filteredTrips[i], isDark)
                                  .animate()
                                  .fadeIn(delay: (i * 50).ms)
                                  .slideY(begin: 0.05),
                        ),
                ),
        ),
      ],
    );
  }

  Widget _filterChip(String label, String value) {
    final selected = _filterStatus == value;
    return GestureDetector(
      onTap: () => setState(() => _filterStatus = value),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary : Colors.grey[200],
          borderRadius: BorderRadius.circular(20),
        ),
        child: Text(
          label,
          style: TextStyle(
            color: selected ? Colors.white : Colors.grey[700],
            fontWeight: FontWeight.w600,
            fontSize: 12,
          ),
        ),
      ),
    );
  }

  Widget _buildTripCard(Map<String, dynamic> trip, bool isDark) {
    final status = trip['status']?.toString() ?? '';
    final cardBg = isDark ? Colors.black.withValues(alpha: 0.3) : Colors.white;
    final tp = isDark ? Colors.white : AppColors.textPrimary;
    final ts = isDark ? Colors.grey[400]! : AppColors.textSecondary;

    return Card(
      margin: const EdgeInsets.only(bottom: 14),
      color: cardBg,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header row
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  trip['request_number'] ?? 'TR-?',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: AppColors.secondaryDark,
                    fontSize: 14,
                  ),
                ),
                _statusBadge(status),
              ],
            ),
            const Divider(height: 20),
            _row(
              Icons.location_on,
              'Pickup',
              trip['pickup_location'] ?? '',
              tp,
              ts,
            ),
            const SizedBox(height: 6),
            _row(
              Icons.flag_rounded,
              'Destination',
              trip['destination'] ?? '',
              tp,
              ts,
            ),
            const SizedBox(height: 6),
            _row(
              Icons.backpack_rounded,
              'Cargo',
              trip['passenger_item_details'] ?? '',
              tp,
              ts,
            ),
            const SizedBox(height: 6),
            _row(
              Icons.calendar_today,
              'Schedule',
              '${trip['requested_date'] ?? ''} @ ${trip['requested_time'] ?? ''}',
              tp,
              ts,
            ),
            if ((trip['special_instructions'] ?? '').toString().isNotEmpty) ...[
              const SizedBox(height: 6),
              _row(
                Icons.info_outline,
                'Notes',
                trip['special_instructions'] ?? '',
                tp,
                ts,
              ),
            ],
            const SizedBox(height: 14),
            _buildStepTracker(status),
            const SizedBox(height: 14),
            _buildActions(trip, status),
          ],
        ),
      ),
    );
  }

  // Step tracker widget inside trip card
  Widget _buildStepTracker(String status) {
    final steps = [
      ('Assigned', 'Dispatcher'),
      ('Accepted', 'You'),
      ('Picked Up', 'You'),
      ('Receipt', 'You'),
      ('Paid', 'Finance'),
    ];
    final rejected = ['DispatcherRejected', 'DriverRejected', 'StaffRejected'];
    if (rejected.contains(status)) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
        decoration: BoxDecoration(
          color: Colors.red[50],
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: Colors.red[200]!),
        ),
        child: Row(
          children: [
            const Icon(Icons.cancel_outlined, color: Colors.red, size: 16),
            const SizedBox(width: 6),
            Text(
              'Rejected',
              style: TextStyle(
                color: Colors.red[700],
                fontWeight: FontWeight.bold,
                fontSize: 12,
              ),
            ),
          ],
        ),
      );
    }
    final idx =
        {
          'DriverAssigned': 0,
          'DriverAccepted': 1,
          'PickedUp': 2,
          'MahberatApprovedPickup': 2,
          'ReceiptSubmitted': 3,
          'ReceiptVerified': 3,
          'StaffApproved': 4,
          'Paid': 4,
        }[status] ??
        0;

    return Row(
      children: List.generate(steps.length * 2 - 1, (i) {
        if (i.isOdd) {
          final lineIdx = i ~/ 2;
          return Expanded(
            child: Container(
              height: 3,
              color: lineIdx < idx ? AppColors.primary : Colors.grey[300],
            ),
          );
        }
        final si = i ~/ 2;
        final done = si < idx;
        final active = si == idx;
        return Column(
          children: [
            Container(
              width: 24,
              height: 24,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: done
                    ? AppColors.primary
                    : active
                    ? Colors.white
                    : Colors.grey[200],
                border: Border.all(
                  color: done || active ? AppColors.primary : Colors.grey[300]!,
                  width: active ? 2.5 : 1.5,
                ),
              ),
              child: Center(
                child: done
                    ? const Icon(Icons.check, size: 12, color: Colors.white)
                    : Text(
                        '${si + 1}',
                        style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                          color: active ? AppColors.primary : Colors.grey[500],
                        ),
                      ),
              ),
            ),
            const SizedBox(height: 3),
            Text(
              steps[si].$1,
              style: TextStyle(
                fontSize: 8,
                color: done || active ? AppColors.primary : Colors.grey[400],
                fontWeight: active ? FontWeight.bold : FontWeight.normal,
              ),
            ),
          ],
        );
      }),
    );
  }

  Widget _buildActions(Map<String, dynamic> trip, String status) {
    switch (status) {
      case 'DriverAssigned':
        return Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: () => _reject(trip),
                icon: const Icon(Icons.close, color: Colors.red, size: 16),
                label: const Text(
                  'Reject',
                  style: TextStyle(color: Colors.red),
                ),
                style: OutlinedButton.styleFrom(
                  side: const BorderSide(color: Colors.red),
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: ElevatedButton.icon(
                onPressed: () => _accept(trip),
                icon: const Icon(Icons.check, color: Colors.white, size: 16),
                label: const Text(
                  'Accept',
                  style: TextStyle(color: Colors.white),
                ),
                style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
              ),
            ),
          ],
        );
      case 'DriverAccepted':
        return SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            onPressed: () => _pickup(trip),
            icon: const Icon(Icons.hail_rounded, color: Colors.white, size: 16),
            label: const Text(
              'Confirm Payload Loaded',
              style: TextStyle(color: Colors.white),
            ),
            style: ElevatedButton.styleFrom(backgroundColor: AppColors.primary),
          ),
        );
      case 'PickedUp':
        // Driver can submit receipt directly after pickup
        return SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            onPressed: () => _submitReceiptPhoto(trip),
            icon: const Icon(
              Icons.receipt_long_rounded,
              color: Colors.white,
              size: 16,
            ),
            label: const Text(
              'Submit Receipt (KG + Photos)',
              style: TextStyle(color: Colors.white),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.secondary,
            ),
          ),
        );
      case 'MahberatApprovedPickup':
        return SizedBox(
          width: double.infinity,
          child: ElevatedButton.icon(
            onPressed: () => _submitReceiptPhoto(trip),
            icon: const Icon(
              Icons.receipt_long_rounded,
              color: Colors.white,
              size: 16,
            ),
            label: const Text(
              'Submit Receipt (KG + Photos)',
              style: TextStyle(color: Colors.white),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.secondary,
            ),
          ),
        );
      default:
        // Completed statuses — show view details button + status message
        final statusMsgs = {
          'ReceiptSubmitted': (
            'Receipt submitted — awaiting Mahberat review.',
            Colors.orange,
          ),
          'ReceiptVerified': (
            'Receipt verified — awaiting staff approval.',
            Colors.purple,
          ),
          'StaffApproved': (
            'Approved — payment being processed.',
            Colors.deepPurple,
          ),
          'Paid': ('Payment complete. Job done!', Colors.green),
        };
        final info = statusMsgs[status];
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Status info row
            Row(
              children: [
                Icon(
                  status == 'Paid'
                      ? Icons.check_circle
                      : Icons.hourglass_bottom_rounded,
                  color: info?.$2 ?? Colors.teal,
                  size: 16,
                ),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    info?.$1 ?? 'Awaiting next action.',
                    style: TextStyle(
                      color: info?.$2 ?? Colors.teal,
                      fontWeight: FontWeight.w600,
                      fontSize: 12,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            // View receipt details button
            OutlinedButton.icon(
              onPressed: () => _viewTripDetails(trip),
              icon: const Icon(Icons.receipt_outlined, size: 16),
              label: const Text('View Trip & Receipt Details'),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.primary,
                side: const BorderSide(color: AppColors.primary),
              ),
            ),
          ],
        );
    }
  }

  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
  // TAB 2 â€” RECEIPT SUBMISSION
  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

  Widget _buildReceiptTab(bool isDark) {
    final user = Provider.of<AuthService>(context).currentUser;
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // â”€â”€ Type switcher â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            _card(
              title: 'Receipt Type & Location',
              icon: Icons.location_on,
              child: Column(
                children: [
                  // Mahberat / Outsource toggle
                  Container(
                    padding: const EdgeInsets.all(4),
                    decoration: BoxDecoration(
                      color: Colors.grey[200],
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Row(
                      children: [
                        Expanded(child: _typeBtn('Mahberat', 'Mahberat')),
                        Expanded(child: _typeBtn('Outsource', 'Outsource')),
                      ],
                    ),
                  ),
                  const SizedBox(height: 14),
                  // Wereda
                  DropdownButtonFormField<int>(
                    decoration: _dec('Wereda', Icons.location_on),
                    value: _selWereda,
                    items: _weredas
                        .map(
                          (w) => DropdownMenuItem<int>(
                            value: w['id'],
                            child: Text(w['name']),
                          ),
                        )
                        .toList(),
                    onChanged: (v) => setState(() => _selWereda = v),
                    validator: (v) => v == null ? 'Required' : null,
                  ),
                  const SizedBox(height: 14),
                  // Mahberat or Company
                  DropdownButtonFormField<int>(
                    decoration: _dec(
                      _receiptType == 'Mahberat' ? 'Mahberat' : 'Company',
                      Icons.business,
                    ),
                    value: _selEntity,
                    items:
                        (_receiptType == 'Mahberat' ? _mahberats : _companies)
                            .map(
                              (m) => DropdownMenuItem<int>(
                                value: m['id'],
                                child: Text(m['name']),
                              ),
                            )
                            .toList(),
                    onChanged: (v) => setState(() => _selEntity = v),
                    validator: (v) => v == null ? 'Required' : null,
                  ),
                  const SizedBox(height: 14),
                  // Vehicle â€” only for driver role
                  if (user?.isDriver == true)
                    DropdownButtonFormField<int>(
                      decoration: _dec(
                        'Vehicle (Optional)',
                        Icons.local_shipping,
                      ),
                      value: _selVehicle,
                      items: _vehicles
                          .map(
                            (v) => DropdownMenuItem<int>(
                              value: v['id'],
                              child: Text(v['name']),
                            ),
                          )
                          .toList(),
                      onChanged: (v) => setState(() => _selVehicle = v),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 14),

            // â”€â”€ KG â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            _card(
              title: 'Work Data',
              icon: Icons.scale,
              child: TextFormField(
                controller: _kgCtrl,
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                decoration: _dec('Kilograms collected', Icons.scale),
                validator: (v) => (v == null || v.isEmpty) ? 'Required' : null,
              ),
            ),
            const SizedBox(height: 14),

            // â”€â”€ Evidence â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            _card(
              title: 'Evidence & Notes',
              icon: Icons.camera_alt,
              child: Column(
                children: [
                  GestureDetector(
                    onTap: _pickImages,
                    child: Container(
                      height: 120,
                      width: double.infinity,
                      decoration: BoxDecoration(
                        color: Colors.grey[100],
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: Colors.grey[300]!),
                      ),
                      child: _images.isNotEmpty
                          ? ListView.builder(
                              scrollDirection: Axis.horizontal,
                              padding: const EdgeInsets.all(8),
                              itemCount: _images.length,
                              itemBuilder: (_, i) => FutureBuilder<Uint8List>(
                                future: _images[i].readAsBytes(),
                                builder: (_, snap) => snap.hasData
                                    ? Padding(
                                        padding: const EdgeInsets.only(
                                          right: 8,
                                        ),
                                        child: ClipRRect(
                                          borderRadius: BorderRadius.circular(
                                            10,
                                          ),
                                          child: Image.memory(
                                            snap.data!,
                                            width: 100,
                                            fit: BoxFit.cover,
                                          ),
                                        ),
                                      )
                                    : const SizedBox(width: 100),
                              ),
                            )
                          : const Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Icon(
                                  Icons.add_a_photo_outlined,
                                  size: 36,
                                  color: AppColors.textHint,
                                ),
                                SizedBox(height: 6),
                                Text(
                                  'Tap to add photos',
                                  style: TextStyle(color: AppColors.textHint),
                                ),
                              ],
                            ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _notesCtrl,
                    maxLines: 3,
                    decoration: _dec('Additional notes', Icons.notes),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // â”€â”€ Submit â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            SizedBox(
              height: 54,
              child: ElevatedButton.icon(
                onPressed: _submitting ? null : _submitReceipt,
                icon: _submitting
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                          color: Colors.white,
                          strokeWidth: 2,
                        ),
                      )
                    : const Icon(Icons.send_rounded, color: Colors.white),
                label: Text(
                  _submitting ? 'Submitting...' : 'SUBMIT RECEIPT',
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                  ),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.secondary,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
              ),
            ),
            const SizedBox(height: 32),
          ],
        ),
      ),
    );
  }

  Widget _typeBtn(String label, String value) {
    final sel = _receiptType == value;
    return GestureDetector(
      onTap: () => setState(() {
        _receiptType = value;
        _selEntity = null;
      }),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(vertical: 11),
        decoration: BoxDecoration(
          color: sel ? Colors.white : Colors.transparent,
          borderRadius: BorderRadius.circular(8),
          boxShadow: sel
              ? [
                  const BoxShadow(
                    color: Colors.black12,
                    blurRadius: 4,
                    offset: Offset(0, 2),
                  ),
                ]
              : null,
        ),
        child: Center(
          child: Text(
            label,
            style: TextStyle(
              fontWeight: FontWeight.bold,
              color: sel ? AppColors.primaryDark : AppColors.textHint,
            ),
          ),
        ),
      ),
    );
  }

  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
  // SHARED HELPERS
  // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

  Widget _card({
    required String title,
    required IconData icon,
    required Widget child,
  }) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icon, color: AppColors.primary, size: 18),
                const SizedBox(width: 8),
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 14),
            child,
          ],
        ),
      ),
    );
  }

  InputDecoration _dec(String label, IconData icon) => InputDecoration(
    labelText: label,
    prefixIcon: Icon(icon, color: AppColors.primary),
    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: BorderSide(color: Colors.grey[300]!),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: const BorderSide(color: AppColors.primary, width: 2),
    ),
    filled: true,
    fillColor: Colors.grey[50],
  );

  Widget _row(IconData icon, String label, String val, Color tp, Color ts) =>
      Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 16, color: AppColors.primary),
          const SizedBox(width: 6),
          Expanded(
            child: RichText(
              text: TextSpan(
                text: '$label: ',
                style: TextStyle(
                  fontWeight: FontWeight.w600,
                  fontSize: 12,
                  color: ts,
                  fontFamily: 'Inter',
                ),
                children: [
                  TextSpan(
                    text: val,
                    style: TextStyle(fontWeight: FontWeight.normal, color: tp),
                  ),
                ],
              ),
            ),
          ),
        ],
      );

  Widget _statusBadge(String status) {
    final map = {
      'DriverAssigned': (Colors.blue, 'Assigned'),
      'DriverAccepted': (Colors.green, 'Accepted'),
      'PickedUp': (Colors.indigo, 'Picked Up'),
      'MahberatApprovedPickup': (Colors.teal, 'Pickup OK'),
      'ReceiptSubmitted': (Colors.orange, 'Receipt Sent'),
      'ReceiptVerified': (Colors.purple, 'Verified'),
      'StaffApproved': (Colors.deepPurple, 'Staff OK'),
      'Paid': (Colors.green, 'Paid âœ“'),
      'DispatcherRejected': (Colors.red, 'Rejected'),
      'DriverRejected': (Colors.red, 'Rejected'),
      'StaffRejected': (Colors.red, 'Rejected'),
    };
    final entry = map[status] ?? (Colors.grey, status);
    final color = entry.$1 as Color;
    final label = entry.$2;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 10,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }

  Widget _emptyState({required IconData icon, required String msg}) => Center(
    child: Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Icon(icon, size: 64, color: Colors.grey[400]),
        const SizedBox(height: 12),
        Text(msg, style: const TextStyle(fontSize: 15, color: Colors.grey)),
        const SizedBox(height: 20),
        ElevatedButton.icon(
          onPressed: _loadTrips,
          icon: const Icon(Icons.refresh),
          label: const Text('Refresh'),
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            foregroundColor: Colors.white,
          ),
        ),
      ],
    ),
  );
}

// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
// TRANSPORT RECEIPT BOTTOM SHEET â€” Full form after PickedUp
// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

class _TransportReceiptSheet extends StatefulWidget {
  final Map<String, dynamic> trip;
  final dynamic user;
  final ApiService apiService;
  final List<Map<String, dynamic>> weredas;
  final List<Map<String, dynamic>> mahberats;
  final void Function(bool success) onDone;

  const _TransportReceiptSheet({
    required this.trip,
    required this.user,
    required this.apiService,
    required this.weredas,
    required this.mahberats,
    required this.onDone,
  });

  @override
  State<_TransportReceiptSheet> createState() => _TransportReceiptSheetState();
}

class _TransportReceiptSheetState extends State<_TransportReceiptSheet> {
  final _formKey = GlobalKey<FormState>();
  final _kgCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  int? _selWereda;
  int? _selMahberat;
  List<XFile> _photos = [];
  bool _loading = false;

  @override
  void initState() {
    super.initState();
    final t = widget.trip;

    // ── Auto-fill Wereda: try ID first, then match by name ──────
    final wid = t['wereda_id'] as int?;
    if (wid != null && widget.weredas.any((w) => w['id'] == wid)) {
      _selWereda = wid;
    } else {
      final wName = (t['wereda_name'] ?? t['wereda'] ?? '')
          .toString()
          .toLowerCase()
          .trim();
      if (wName.isNotEmpty) {
        final match = widget.weredas.firstWhere(
          (w) => w['name'].toString().toLowerCase().trim() == wName,
          orElse: () => {},
        );
        if (match.isNotEmpty) _selWereda = match['id'] as int?;
      }
    }

    // ── Auto-fill Mahberat: try ID first, then match by name ────
    final mid = t['mahberat_id'] as int?;
    if (mid != null && widget.mahberats.any((m) => m['id'] == mid)) {
      _selMahberat = mid;
    } else {
      final mName = (t['mahberat_name'] ?? t['mahberat'] ?? '')
          .toString()
          .toLowerCase()
          .trim();
      if (mName.isNotEmpty) {
        final match = widget.mahberats.firstWhere(
          (m) => m['name'].toString().toLowerCase().trim() == mName,
          orElse: () => {},
        );
        if (match.isNotEmpty) _selMahberat = match['id'] as int?;
      }
    }

    // ── Auto-fill KG from passenger_item_details if numeric ─────
    final details = (t['passenger_item_details'] ?? '').toString();
    final kgMatch = RegExp(
      r'(\d+(?:\.\d+)?)\s*(?:kg|kilo)',
      caseSensitive: false,
    ).firstMatch(details);
    if (kgMatch != null) {
      _kgCtrl.text = kgMatch.group(1) ?? '';
    }
  }

  @override
  void dispose() {
    _kgCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  // ── Photo pickers ──────────────────────────────────────────────
  Future<void> _takePhoto() async {
    final p = await ImagePicker().pickImage(
      source: ImageSource.camera,
      imageQuality: 85,
    );
    if (p != null && mounted) setState(() => _photos.add(p));
  }

  Future<void> _pickGallery() async {
    final picked = await ImagePicker().pickMultiImage(imageQuality: 85);
    if (picked.isNotEmpty && mounted) setState(() => _photos.addAll(picked));
  }

  // ── Submit ─────────────────────────────────────────────────────
  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);

    List<String> urls = [];
    for (final p in _photos) {
      final u = await widget.apiService.uploadImage(p);
      if (u != null) urls.add(u);
    }

    final ok = await widget.apiService.submitTransportReceipt(
      widget.trip['id'],
      driverId: widget.user.id,
      driverName: widget.user.name,
      receiptPhotoUrl: urls.isNotEmpty ? urls.first : null,
      notes: urls.length > 1
          ? '${_notesCtrl.text.trim()} | extra_photos:${urls.skip(1).join(',')}'
                .trim()
          : _notesCtrl.text,
      actualKilogram: double.tryParse(_kgCtrl.text),
      weredaId: _selWereda,
      mahberatId: _selMahberat,
    );

    if (mounted) {
      Navigator.pop(context);
      widget.onDone(ok);
    }
  }

  // ── Helpers ────────────────────────────────────────────────────
  InputDecoration _dec(String label, IconData icon) => InputDecoration(
    labelText: label,
    prefixIcon: Icon(icon, color: AppColors.primary),
    border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
    enabledBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: BorderSide(color: Colors.grey[300]!),
    ),
    focusedBorder: OutlineInputBorder(
      borderRadius: BorderRadius.circular(12),
      borderSide: const BorderSide(color: AppColors.primary, width: 2),
    ),
    filled: true,
    fillColor: Colors.grey[50],
  );

  Widget _lbl(String text) => Padding(
    padding: const EdgeInsets.only(bottom: 8, top: 4),
    child: Text(
      text,
      style: const TextStyle(
        fontSize: 13,
        fontWeight: FontWeight.w700,
        color: AppColors.textSecondary,
      ),
    ),
  );

  Widget _tripInfoRow(IconData icon, String label, String value) => Row(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Icon(icon, size: 14, color: AppColors.primary),
      const SizedBox(width: 6),
      Expanded(
        child: RichText(
          text: TextSpan(
            text: '$label: ',
            style: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: AppColors.textSecondary,
              fontFamily: 'Inter',
            ),
            children: [
              TextSpan(
                text: value,
                style: const TextStyle(
                  fontWeight: FontWeight.w500,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
        ),
      ),
    ],
  );

  Widget _infoChip(IconData icon, String label) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
    decoration: BoxDecoration(
      color: Colors.white.withValues(alpha: 0.2),
      borderRadius: BorderRadius.circular(20),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 13, color: Colors.white),
        const SizedBox(width: 5),
        Flexible(
          child: Text(
            label,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 11,
              fontWeight: FontWeight.w600,
            ),
            overflow: TextOverflow.ellipsis,
            maxLines: 1,
          ),
        ),
      ],
    ),
  );

  // ── BUILD ──────────────────────────────────────────────────────
  @override
  Widget build(BuildContext context) {
    final trip = widget.trip;
    final user = widget.user;

    return DraggableScrollableSheet(
      initialChildSize: 0.93,
      minChildSize: 0.5,
      maxChildSize: 0.97,
      builder: (_, ctrl) => Container(
        decoration: const BoxDecoration(
          color: Color(0xFFF0F4F3),
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        ),
        child: Column(
          children: [
            // Drag handle
            Container(
              margin: const EdgeInsets.symmetric(vertical: 10),
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: Colors.grey[300],
                borderRadius: BorderRadius.circular(2),
              ),
            ),

            // Header banner (gradient with auto-filled info chips)
            Container(
              padding: const EdgeInsets.fromLTRB(20, 4, 20, 16),
              decoration: const BoxDecoration(
                gradient: AppColors.cardGradient,
                borderRadius: BorderRadius.vertical(
                  bottom: Radius.circular(20),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      const Icon(
                        Icons.receipt_long,
                        color: Colors.white,
                        size: 22,
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Submit Trip Receipt',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 18,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                            Text(
                              trip['request_number'] ?? '',
                              style: TextStyle(
                                color: Colors.white.withValues(alpha: 0.75),
                                fontSize: 12,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  // Auto-filled info chips
                  Wrap(
                    spacing: 8,
                    runSpacing: 6,
                    children: [
                      _infoChip(Icons.person_outline, user.name),
                      _infoChip(
                        Icons.local_shipping_outlined,
                        user.vehicleName ?? 'No Vehicle',
                      ),
                      _infoChip(
                        Icons.location_on_outlined,
                        trip['pickup_location'] ?? '',
                      ),
                      _infoChip(Icons.flag_outlined, trip['destination'] ?? ''),
                    ],
                  ),
                ],
              ),
            ),

            // Scrollable form
            Expanded(
              child: SingleChildScrollView(
                controller: ctrl,
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      // ── Trip info (read-only) ───────────────────────
                      Container(
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(
                          color: AppColors.primary.withValues(alpha: 0.06),
                          borderRadius: BorderRadius.circular(14),
                          border: Border.all(
                            color: AppColors.primary.withValues(alpha: 0.2),
                          ),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                const Icon(
                                  Icons.local_shipping_outlined,
                                  size: 15,
                                  color: AppColors.primary,
                                ),
                                const SizedBox(width: 6),
                                const Text(
                                  'TRANSPORT REQUEST INFO',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.w700,
                                    color: AppColors.primary,
                                    letterSpacing: 0.8,
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 10),
                            _tripInfoRow(
                              Icons.location_on_outlined,
                              'Pickup',
                              trip['pickup_location'] ?? '—',
                            ),
                            const SizedBox(height: 6),
                            _tripInfoRow(
                              Icons.flag_outlined,
                              'Destination',
                              trip['destination'] ?? '—',
                            ),
                            const SizedBox(height: 6),
                            _tripInfoRow(
                              Icons.backpack_outlined,
                              'Cargo',
                              trip['passenger_item_details'] ?? '—',
                            ),
                            if ((trip['mahberat_name'] ?? '')
                                .toString()
                                .isNotEmpty) ...[
                              const SizedBox(height: 6),
                              _tripInfoRow(
                                Icons.business_outlined,
                                'Mahberat',
                                trip['mahberat_name'] ?? '—',
                              ),
                            ],
                            const SizedBox(height: 6),
                            _tripInfoRow(
                              Icons.calendar_today_outlined,
                              'Schedule',
                              '${trip['requested_date'] ?? ''} @ ${trip['requested_time'] ?? ''}',
                            ),
                            if ((trip['special_instructions'] ?? '')
                                .toString()
                                .isNotEmpty) ...[
                              const SizedBox(height: 6),
                              _tripInfoRow(
                                Icons.info_outline,
                                'Instructions',
                                trip['special_instructions'] ?? '',
                              ),
                            ],
                          ],
                        ),
                      ),
                      const SizedBox(height: 18),

                      // ── KG ─────────────────────────────────────────
                      _lbl('Actual Weight Collected *'),
                      TextFormField(
                        controller: _kgCtrl,
                        keyboardType: const TextInputType.numberWithOptions(
                          decimal: true,
                        ),
                        decoration: _dec('Kilograms (KG)', Icons.scale),
                        validator: (v) =>
                            (v == null || v.isEmpty) ? 'Enter actual KG' : null,
                      ),
                      const SizedBox(height: 16),

                      // ── Wereda / Mahberat (auto-filled if available) ──
                      _lbl('Collection Location *'),
                      // Auto-fill notice
                      if (_selWereda != null || _selMahberat != null)
                        Container(
                          margin: const EdgeInsets.only(bottom: 10),
                          padding: const EdgeInsets.symmetric(
                            horizontal: 10,
                            vertical: 6,
                          ),
                          decoration: BoxDecoration(
                            color: Colors.green[50],
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(color: Colors.green[200]!),
                          ),
                          child: Row(
                            children: [
                              Icon(
                                Icons.auto_fix_high,
                                size: 14,
                                color: Colors.green[700],
                              ),
                              const SizedBox(width: 6),
                              Text(
                                'Auto-filled from transport request',
                                style: TextStyle(
                                  fontSize: 11,
                                  color: Colors.green[700],
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ],
                          ),
                        ),
                      DropdownButtonFormField<int>(
                        decoration: _dec('Wereda', Icons.location_on),
                        value: _selWereda,
                        items: widget.weredas
                            .map(
                              (w) => DropdownMenuItem<int>(
                                value: w['id'],
                                child: Text(w['name']),
                              ),
                            )
                            .toList(),
                        onChanged: (v) => setState(() => _selWereda = v),
                        validator: (v) => v == null ? 'Required' : null,
                      ),
                      const SizedBox(height: 12),
                      DropdownButtonFormField<int>(
                        decoration: _dec('Mahberat', Icons.business),
                        value: _selMahberat,
                        items: widget.mahberats
                            .map(
                              (m) => DropdownMenuItem<int>(
                                value: m['id'],
                                child: Text(m['name']),
                              ),
                            )
                            .toList(),
                        onChanged: (v) => setState(() => _selMahberat = v),
                        validator: (v) => v == null ? 'Required' : null,
                      ),
                      const SizedBox(height: 16),

                      // ── Photos (multiple) ──────────────────────────
                      _lbl('Receipt Photos (multiple allowed)'),
                      // Photo preview strip
                      if (_photos.isNotEmpty) ...[
                        SizedBox(
                          height: 108,
                          child: ListView.builder(
                            scrollDirection: Axis.horizontal,
                            itemCount: _photos.length,
                            itemBuilder: (_, i) => Stack(
                              children: [
                                FutureBuilder<Uint8List>(
                                  future: _photos[i].readAsBytes(),
                                  builder: (_, snap) => snap.hasData
                                      ? Container(
                                          width: 100,
                                          height: 100,
                                          margin: const EdgeInsets.only(
                                            right: 8,
                                          ),
                                          child: ClipRRect(
                                            borderRadius: BorderRadius.circular(
                                              12,
                                            ),
                                            child: Image.memory(
                                              snap.data!,
                                              fit: BoxFit.cover,
                                            ),
                                          ),
                                        )
                                      : const SizedBox(width: 100),
                                ),
                                Positioned(
                                  top: 2,
                                  right: 10,
                                  child: GestureDetector(
                                    onTap: () =>
                                        setState(() => _photos.removeAt(i)),
                                    child: Container(
                                      width: 22,
                                      height: 22,
                                      decoration: const BoxDecoration(
                                        color: Colors.red,
                                        shape: BoxShape.circle,
                                      ),
                                      child: const Icon(
                                        Icons.close,
                                        color: Colors.white,
                                        size: 14,
                                      ),
                                    ),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                        Padding(
                          padding: const EdgeInsets.symmetric(vertical: 6),
                          child: Text(
                            '${_photos.length} photo(s) selected',
                            style: const TextStyle(
                              color: AppColors.primary,
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ],
                      // Camera + Gallery buttons
                      Row(
                        children: [
                          Expanded(
                            child: OutlinedButton.icon(
                              onPressed: _takePhoto,
                              icon: const Icon(Icons.camera_alt, size: 16),
                              label: const Text('Camera'),
                              style: OutlinedButton.styleFrom(
                                foregroundColor: AppColors.primary,
                                side: const BorderSide(
                                  color: AppColors.primary,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: OutlinedButton.icon(
                              onPressed: _pickGallery,
                              icon: const Icon(
                                Icons.photo_library_outlined,
                                size: 16,
                              ),
                              label: const Text('Gallery (multi)'),
                              style: OutlinedButton.styleFrom(
                                foregroundColor: AppColors.primary,
                                side: const BorderSide(
                                  color: AppColors.primary,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),

                      // ── Notes ──────────────────────────────────────
                      _lbl('Notes (optional)'),
                      TextFormField(
                        controller: _notesCtrl,
                        maxLines: 3,
                        decoration: _dec('Additional notes', Icons.notes),
                      ),
                      const SizedBox(height: 24),

                      // ── Submit button ──────────────────────────────
                      SizedBox(
                        height: 54,
                        child: ElevatedButton.icon(
                          onPressed: _loading ? null : _submit,
                          icon: _loading
                              ? const SizedBox(
                                  width: 20,
                                  height: 20,
                                  child: CircularProgressIndicator(
                                    color: Colors.white,
                                    strokeWidth: 2.5,
                                  ),
                                )
                              : const Icon(
                                  Icons.send_rounded,
                                  color: Colors.white,
                                ),
                          label: Text(
                            _loading ? 'Submitting...' : 'SUBMIT RECEIPT',
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                              color: Colors.white,
                            ),
                          ),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.primary,
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(14),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ══════════════════════════════════════════════════════════════════════════════
// TRIP DETAIL SHEET — Read-only view after receipt submitted
// ══════════════════════════════════════════════════════════════════════════════

class _TripDetailSheet extends StatelessWidget {
  final Map<String, dynamic> trip;
  const _TripDetailSheet({required this.trip});

  Widget _row(String label, String? value, {IconData? icon}) => Padding(
    padding: const EdgeInsets.only(bottom: 12),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (icon != null) ...[
          Icon(icon, size: 16, color: AppColors.primary),
          const SizedBox(width: 8),
        ],
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: const TextStyle(
                  fontSize: 11,
                  color: AppColors.textSecondary,
                  fontWeight: FontWeight.w500,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                value?.isNotEmpty == true ? value! : '—',
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );

  @override
  Widget build(BuildContext context) {
    final status = trip['status']?.toString() ?? '';
    final statusColors = {
      'ReceiptSubmitted': Colors.orange,
      'ReceiptVerified': Colors.purple,
      'StaffApproved': Colors.deepPurple,
      'Paid': Colors.green,
    };
    final statusColor = statusColors[status] ?? Colors.teal;

    return DraggableScrollableSheet(
      initialChildSize: 0.85,
      minChildSize: 0.4,
      maxChildSize: 0.95,
      builder: (_, ctrl) => Container(
        decoration: const BoxDecoration(
          color: Color(0xFFF0F4F3),
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        ),
        child: Column(
          children: [
            // Drag handle
            Container(
              margin: const EdgeInsets.symmetric(vertical: 10),
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: Colors.grey[300],
                borderRadius: BorderRadius.circular(2),
              ),
            ),

            // Header
            Container(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 16),
              decoration: const BoxDecoration(
                gradient: AppColors.cardGradient,
                borderRadius: BorderRadius.vertical(
                  bottom: Radius.circular(20),
                ),
              ),
              child: Row(
                children: [
                  const Icon(Icons.receipt_long, color: Colors.white, size: 22),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Trip & Receipt Details',
                          style: TextStyle(
                            color: Colors.white,
                            fontSize: 17,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        Text(
                          trip['request_number'] ?? '',
                          style: TextStyle(
                            color: Colors.white.withValues(alpha: 0.75),
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 5,
                    ),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.2),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      status
                          .replaceAllMapped(
                            RegExp(r'([A-Z])'),
                            (m) => ' ${m[0]}',
                          )
                          .trim(),
                      style: TextStyle(
                        color: statusColor == Colors.green
                            ? Colors.white
                            : Colors.white,
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            // Content
            Expanded(
              child: SingleChildScrollView(
                controller: ctrl,
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // ── Trip Info ──────────────────────────────────────
                    const Text(
                      'TRIP INFORMATION',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textSecondary,
                        letterSpacing: 1.2,
                      ),
                    ),
                    const SizedBox(height: 12),
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(14),
                        boxShadow: [
                          BoxShadow(color: AppColors.shadow, blurRadius: 6),
                        ],
                      ),
                      child: Column(
                        children: [
                          _row(
                            'Pickup Location',
                            trip['pickup_location'],
                            icon: Icons.location_on,
                          ),
                          _row(
                            'Destination',
                            trip['destination'],
                            icon: Icons.flag_rounded,
                          ),
                          _row(
                            'Cargo / Passengers',
                            trip['passenger_item_details'],
                            icon: Icons.backpack_rounded,
                          ),
                          _row(
                            'Scheduled',
                            '${trip['requested_date'] ?? ''} @ ${trip['requested_time'] ?? ''}',
                            icon: Icons.calendar_today,
                          ),
                          if ((trip['driver_name'] ?? '').toString().isNotEmpty)
                            _row(
                              'Driver',
                              trip['driver_name'],
                              icon: Icons.person_outline,
                            ),
                          if ((trip['vehicle_plate'] ?? '')
                              .toString()
                              .isNotEmpty)
                            _row(
                              'Vehicle',
                              trip['vehicle_plate'],
                              icon: Icons.local_shipping_outlined,
                            ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 20),

                    // ── Receipt Info ───────────────────────────────────
                    const Text(
                      'RECEIPT DETAILS',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textSecondary,
                        letterSpacing: 1.2,
                      ),
                    ),
                    const SizedBox(height: 12),
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(14),
                        boxShadow: [
                          BoxShadow(color: AppColors.shadow, blurRadius: 6),
                        ],
                      ),
                      child: Column(
                        children: [
                          _row(
                            'Actual Weight (KG)',
                            trip['actual_kilogram'] != null
                                ? '${trip['actual_kilogram']} KG'
                                : null,
                            icon: Icons.scale,
                          ),
                          _row(
                            'Receipt Notes',
                            trip['receipt_notes'],
                            icon: Icons.notes,
                          ),
                          _row(
                            'Transport Cost',
                            trip['transport_cost'] != null
                                ? 'ETB ${trip['transport_cost']}'
                                : null,
                            icon: Icons.payments_outlined,
                          ),
                          _row(
                            'Transaction #',
                            trip['transaction_number'],
                            icon: Icons.tag,
                          ),
                        ],
                      ),
                    ),

                    // ── Receipt photo if available ─────────────────────
                    if ((trip['receipt_photo_url'] ?? '')
                        .toString()
                        .isNotEmpty) ...[
                      const SizedBox(height: 20),
                      const Text(
                        'RECEIPT PHOTO',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textSecondary,
                          letterSpacing: 1.2,
                        ),
                      ),
                      const SizedBox(height: 12),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(14),
                        child: Image.network(
                          trip['receipt_photo_url'].toString().startsWith(
                                'http',
                              )
                              ? trip['receipt_photo_url']
                              : '${ApiService.transportApiBaseUrl.replaceAll('/api/transport', '')}${trip['receipt_photo_url']}',
                          fit: BoxFit.contain,
                          errorBuilder: (_, __, ___) => Container(
                            height: 120,
                            color: Colors.grey[100],
                            child: const Center(
                              child: Icon(
                                Icons.broken_image_outlined,
                                color: Colors.grey,
                                size: 40,
                              ),
                            ),
                          ),
                        ),
                      ),
                    ],

                    // ── Status timeline ────────────────────────────────
                    const SizedBox(height: 20),
                    const Text(
                      'STATUS',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textSecondary,
                        letterSpacing: 1.2,
                      ),
                    ),
                    const SizedBox(height: 12),
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: statusColor.withValues(alpha: 0.08),
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(
                          color: statusColor.withValues(alpha: 0.3),
                        ),
                      ),
                      child: Row(
                        children: [
                          Icon(
                            status == 'Paid'
                                ? Icons.check_circle
                                : Icons.schedule,
                            color: statusColor,
                            size: 24,
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  status
                                      .replaceAllMapped(
                                        RegExp(r'([A-Z])'),
                                        (m) => ' ${m[0]}',
                                      )
                                      .trim(),
                                  style: TextStyle(
                                    color: statusColor,
                                    fontWeight: FontWeight.w800,
                                    fontSize: 15,
                                  ),
                                ),
                                Text(
                                  {
                                        'ReceiptSubmitted':
                                            'Mahberat is reviewing your receipt.',
                                        'ReceiptVerified':
                                            'Staff is approving the payment.',
                                        'StaffApproved':
                                            'Finance is processing payment.',
                                        'Paid': 'Payment has been completed.',
                                      }[status] ??
                                      '',
                                  style: TextStyle(
                                    color: statusColor.withValues(alpha: 0.8),
                                    fontSize: 12,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 32),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
