import 'dart:async';
import 'dart:math' as math;

import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/providers/ticket_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:ecommerce_mobile/utils/ticket_code.dart';
import 'package:ecommerce_mobile/widgets/ticket_verification_result.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:image_picker/image_picker.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:provider/provider.dart';

class ScanTicketScreen extends StatefulWidget {
  const ScanTicketScreen({
    super.key,
    required this.isActive,
    required this.onEnterCode,
  });

  final bool isActive;
  final VoidCallback onEnterCode;

  @override
  State<ScanTicketScreen> createState() => _ScanTicketScreenState();
}

class _ScanTicketScreenState extends State<ScanTicketScreen> with WidgetsBindingObserver {
  static const _cooldown = Duration(seconds: 4);

  late final MobileScannerController _controller;
  final ImagePicker _imagePicker = ImagePicker();

  bool _disposed = false;
  bool _appResumed = true;
  bool _busy = false;
  bool _picking = false;
  bool _holdCamera = false;
  Ticket? _ticket;
  String? _error;
  String? _invalidMessage;
  String? _cooldownCode;
  DateTime? _cooldownUntil;
  Timer? _invalidTimer;
  Future<void> _cameraChain = Future<void>.value();
  bool _stopScheduled = false;

  bool get _showResult => _busy || _ticket != null || _error != null;

  bool get _shouldRunCamera =>
      widget.isActive && _appResumed && !_holdCamera && !_picking && !_busy;

  @override
  void initState() {
    super.initState();
    final lifecycle = WidgetsBinding.instance.lifecycleState;
    _appResumed = lifecycle == null || lifecycle == AppLifecycleState.resumed;
    WidgetsBinding.instance.addObserver(this);
    _controller = MobileScannerController(
      // The widget starts the camera after its own debug-mode reset.
      autoStart: true,
      facing: CameraFacing.back,
      formats: const [BarcodeFormat.qrCode],
      detectionSpeed: DetectionSpeed.normal,
    );
    _controller.addListener(_onCameraState);
  }

  void _onCameraState() {
    if (_disposed || !mounted || _stopScheduled) return;
    if (!_shouldRunCamera && _controller.value.isRunning) {
      _stopScheduled = true;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        _stopScheduled = false;
        if (mounted) _syncCamera();
      });
    }
  }

  @override
  void didUpdateWidget(covariant ScanTicketScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.isActive != widget.isActive) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) _syncCamera();
      });
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.inactive) return;
    final resumed = state == AppLifecycleState.resumed;
    if (resumed == _appResumed) return;
    _appResumed = resumed;
    _syncCamera();
  }

  @override
  void dispose() {
    _disposed = true;
    _invalidTimer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    _controller.removeListener(_onCameraState);
    _controller.dispose();
    super.dispose();
  }

  void _syncCamera() {
    _cameraChain = _cameraChain.then((_) async {
      try {
        await _applyCamera();
      } catch (_) {}
    });
  }

  Future<void> _applyCamera() async {
    if (!mounted || _disposed) return;
    final state = _controller.value;
    try {
      if (_shouldRunCamera) {
        if (!state.isRunning && !state.isStarting) {
          await _controller.start();
        }
      } else if (state.isRunning) {
        await _controller.stop();
      }
    } on MobileScannerException {
      // The preview's errorBuilder shows permission and availability failures.
    }
    if (mounted) setState(() {});
  }

  void _onDetect(BarcodeCapture capture) {
    if (!widget.isActive || _busy || _holdCamera || _picking) return;
    final raw = _firstRaw(capture);
    if (raw == null) return;
    final code = normalizeTicketCode(raw);
    if (code.isEmpty || _isCoolingDown(code)) return;
    if (!isWildNestTicketCode(code)) {
      _armCooldown(code);
      _showInvalid('This QR code is not a WildNest ticket');
      return;
    }
    _verify(code);
  }

  Future<void> _scanFromImage() async {
    if (_busy || _picking) return;
    setState(() => _picking = true);
    _syncCamera();
    try {
      final image = await _imagePicker.pickImage(
        source: ImageSource.gallery,
        requestFullMetadata: false,
      );
      if (!mounted || image == null) return;
      final capture = await _controller.analyzeImage(
        image.path,
        formats: const [BarcodeFormat.qrCode],
      );
      if (!mounted) return;
      final raw = _firstRaw(capture);
      if (raw == null || raw.trim().isEmpty) {
        _showInvalid('No QR code was found in that image.');
        return;
      }
      final code = normalizeTicketCode(raw);
      if (!isWildNestTicketCode(code)) {
        _showInvalid('This QR code is not a WildNest ticket');
        return;
      }
      await _verify(code);
    } on PlatformException {
      if (mounted) _showInvalid('Photo library access was denied.');
    } on UnsupportedError {
      if (mounted) {
        _showInvalid('Scanning an image is not supported on this device.');
      }
    } catch (_) {
      if (mounted) _showInvalid('Could not read a QR code from that image.');
    } finally {
      if (mounted) {
        setState(() => _picking = false);
        _syncCamera();
      } else {
        _picking = false;
      }
    }
  }

  Future<void> _verify(String code) async {
    if (_busy || !mounted) return;
    _armCooldown(code);
    setState(() {
      _busy = true;
      _holdCamera = true;
      _ticket = null;
      _error = null;
      _invalidMessage = null;
    });
    _syncCamera();

    final tickets = context.read<TicketProvider>();
    try {
      final ticket = await tickets.verify(
        code,
        method: TicketVerificationMethod.qrScan,
      );
      if (!mounted) return;
      setState(() => _ticket = ticket);
      await HapticFeedback.lightImpact();
    } on SessionExpiredException {
      _holdCamera = false;
    } on ApiClientException catch (e) {
      if (!mounted) return;
      setState(() => _error = e.message);
      await HapticFeedback.heavyImpact();
    } catch (_) {
      if (!mounted) return;
      setState(() => _error = 'Could not verify this ticket. Please try again.');
      await HapticFeedback.heavyImpact();
    } finally {
      _armCooldown(code);
      if (mounted) {
        setState(() => _busy = false);
        _syncCamera();
      }
    }
  }

  Future<void> _toggleTorch() async {
    try {
      await _controller.toggleTorch();
    } catch (_) {}
  }

  Future<void> _switchCamera() async {
    try {
      await _controller.switchCamera();
    } catch (_) {}
  }

  void _scanNext() {
    setState(() {
      _ticket = null;
      _error = null;
      _holdCamera = false;
    });
    _syncCamera();
  }

  void _armCooldown(String code) {
    _cooldownCode = code;
    _cooldownUntil = DateTime.now().add(_cooldown);
  }

  bool _isCoolingDown(String code) {
    final until = _cooldownUntil;
    return _cooldownCode == code && until != null && DateTime.now().isBefore(until);
  }

  void _showInvalid(String message) {
    _invalidTimer?.cancel();
    if (!mounted) return;
    setState(() => _invalidMessage = message);
    _invalidTimer = Timer(const Duration(seconds: 3), () {
      if (!mounted) return;
      setState(() => _invalidMessage = null);
    });
  }

  String? _firstRaw(BarcodeCapture? capture) {
    final barcodes = capture?.barcodes;
    if (barcodes == null || barcodes.isEmpty) return null;
    return barcodes.first.rawValue;
  }

  Rect _scanWindow(BoxConstraints constraints) {
    final side = math.min(constraints.maxWidth, constraints.maxHeight) * 0.68;
    return Rect.fromCenter(
      center: Offset(constraints.maxWidth / 2, constraints.maxHeight / 2),
      width: side,
      height: side,
    );
  }

  Color get _holdColor {
    if (_error != null) return AppColors.heart.withValues(alpha: 0.16);
    if (_ticket != null) return AppColors.mint;
    return AppColors.cream;
  }

  String _cameraMessage(MobileScannerException error) {
    switch (error.errorCode) {
      case MobileScannerErrorCode.permissionDenied:
        return 'Camera access is turned off. Allow the camera to scan tickets, or enter the code instead.';
      case MobileScannerErrorCode.unsupported:
        return 'The camera is not available on this device. You can enter the ticket code instead.';
      default:
        return 'The camera could not be started. You can try again or enter the ticket code.';
    }
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        return Stack(
          children: [
            Column(
              children: [
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 4, 16, 0),
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(24),
                      child: _preview(),
                    ),
                  ),
                ),
                if (!_showResult) _toolbar(),
              ],
            ),
            if (_showResult)
              Positioned(
                left: 0,
                right: 0,
                bottom: 0,
                child: ConstrainedBox(
                  constraints: BoxConstraints(maxHeight: constraints.maxHeight * 0.72),
                  child: _ResultSheet(
                    child: TicketVerificationResult(
                      loading: _busy,
                      ticket: _ticket,
                      error: _error,
                      onNext: _busy ? null : _scanNext,
                    ),
                  ),
                ),
              ),
          ],
        );
      },
    );
  }

  Widget _preview() {
    return LayoutBuilder(
      builder: (context, constraints) {
        final window = _scanWindow(constraints);
        return Stack(
          fit: StackFit.expand,
          children: [
            MobileScanner(
              controller: _controller,
              scanWindow: window,
              fit: BoxFit.cover,
              useAppLifecycleState: false,
              onDetect: _onDetect,
              placeholderBuilder: (_) => const ColoredBox(
                color: AppColors.cream,
                child: Center(
                  child: CircularProgressIndicator(color: AppColors.sage),
                ),
              ),
              errorBuilder: (context, error) => _CameraUnavailable(
                message: _cameraMessage(error),
                onRetry: _syncCamera,
                onEnterCode: widget.onEnterCode,
              ),
              overlayBuilder: (context, _) {
                final state = _controller.value;
                if (_holdCamera || _busy || state.error != null || !state.isRunning) {
                  return const SizedBox.shrink();
                }
                return SizedBox.expand(
                  child: Stack(
                    children: [
                      CustomPaint(
                        painter: _ScanWindowPainter(window),
                        child: const SizedBox.expand(),
                      ),
                      Positioned(
                        top: window.bottom + 14,
                        left: 16,
                        right: 16,
                        child: const Text(
                          'Align the QR code',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.w700,
                            shadows: [Shadow(blurRadius: 8, color: Color(0xCC000000))],
                          ),
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
            if (_holdCamera || _busy) ColoredBox(color: _holdColor),
            if (_invalidMessage != null && !_holdCamera)
              Positioned(
                top: 12,
                left: 12,
                right: 12,
                child: _InvalidBanner(message: _invalidMessage!),
              ),
          ],
        );
      },
    );
  }

  Widget _toolbar() {
    return ListenableBuilder(
      listenable: _controller,
      builder: (context, _) {
        final state = _controller.value;
        final cameraReady = state.isRunning && state.error == null;
        final torchOn = state.torchState == TorchState.on;
        final torchUnavailable = state.torchState == TorchState.unavailable;
        final canFlip = state.availableCameras == null || state.availableCameras! > 1;
        return Padding(
          padding: const EdgeInsets.fromLTRB(20, 12, 20, 12),
          child: Row(
            children: [
              Expanded(
                child: _ToolButton(
                  icon: torchOn ? Icons.flash_on_rounded : Icons.flash_off_rounded,
                  label: 'Light',
                  selected: torchOn,
                  onPressed: cameraReady && !torchUnavailable ? _toggleTorch : null,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _ToolButton(
                  icon: Icons.cameraswitch_outlined,
                  label: 'Flip',
                  onPressed: cameraReady && canFlip ? _switchCamera : null,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: _ToolButton(
                  icon: Icons.photo_outlined,
                  label: 'Image',
                  onPressed: _busy || _picking ? null : _scanFromImage,
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _ResultSheet extends StatelessWidget {
  const _ResultSheet({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.surface,
      elevation: 16,
      shadowColor: const Color(0x33000000),
      borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
      child: SingleChildScrollView(
        padding: const EdgeInsets.fromLTRB(16, 10, 16, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 42,
              height: 4,
              margin: const EdgeInsets.only(bottom: 12),
              decoration: BoxDecoration(
                color: AppColors.border,
                borderRadius: BorderRadius.circular(4),
              ),
            ),
            child,
          ],
        ),
      ),
    );
  }
}

class _InvalidBanner extends StatelessWidget {
  const _InvalidBanner({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(14),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        child: Row(
          children: [
            const Icon(Icons.qr_code_2_rounded, color: AppColors.heart),
            const SizedBox(width: 10),
            Expanded(
              child: Text(
                message,
                style: const TextStyle(fontWeight: FontWeight.w700, height: 1.3),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CameraUnavailable extends StatelessWidget {
  const _CameraUnavailable({
    required this.message,
    required this.onRetry,
    required this.onEnterCode,
  });

  final String message;
  final VoidCallback onRetry;
  final VoidCallback onEnterCode;

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: AppColors.cream,
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.no_photography_outlined, size: 42, color: AppColors.sage),
            const SizedBox(height: 12),
            Text(
              message,
              textAlign: TextAlign.center,
              style: const TextStyle(color: AppColors.text, height: 1.35),
            ),
            const SizedBox(height: 16),
            ElevatedButton(onPressed: onRetry, child: const Text('Try again')),
            TextButton(onPressed: onEnterCode, child: const Text('Enter code')),
          ],
        ),
      ),
    );
  }
}

class _ToolButton extends StatelessWidget {
  const _ToolButton({
    required this.icon,
    required this.label,
    required this.onPressed,
    this.selected = false,
  });

  final IconData icon;
  final String label;
  final VoidCallback? onPressed;
  final bool selected;

  @override
  Widget build(BuildContext context) {
    final enabled = onPressed != null;
    final foreground = !enabled
        ? AppColors.navUnselected
        : selected
        ? Colors.white
        : AppColors.sageDark;
    return Material(
      color: selected ? AppColors.sage : AppColors.surface,
      borderRadius: BorderRadius.circular(16),
      child: InkWell(
        onTap: onPressed,
        borderRadius: BorderRadius.circular(16),
        child: Container(
          height: 64,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: selected ? AppColors.sage : AppColors.border),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, color: foreground, size: 22),
              const SizedBox(height: 4),
              Text(
                label,
                style: TextStyle(
                  color: foreground,
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ScanWindowPainter extends CustomPainter {
  const _ScanWindowPainter(this.window);

  final Rect window;

  @override
  void paint(Canvas canvas, Size size) {
    final overlay = Path()..addRect(Offset.zero & size);
    final hole = Path()..addRRect(RRect.fromRectAndRadius(window, const Radius.circular(20)));
    final shade = Path.combine(PathOperation.difference, overlay, hole);
    canvas.drawPath(shade, Paint()..color = const Color(0x880F1A12));
    final border = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3
      ..color = Colors.white;
    canvas.drawRRect(RRect.fromRectAndRadius(window, const Radius.circular(20)), border);

    const corner = 22.0;
    final accent = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 4
      ..strokeCap = StrokeCap.round
      ..color = AppColors.mint;
    void cornerPath(Offset origin, bool right, bool down) {
      final dx = right ? -corner : corner;
      final dy = down ? -corner : corner;
      final path = Path()
        ..moveTo(origin.dx + dx, origin.dy)
        ..lineTo(origin.dx, origin.dy)
        ..lineTo(origin.dx, origin.dy + dy);
      canvas.drawPath(path, accent);
    }

    cornerPath(window.topLeft, false, false);
    cornerPath(window.topRight, true, false);
    cornerPath(window.bottomLeft, false, true);
    cornerPath(window.bottomRight, true, true);
  }

  @override
  bool shouldRepaint(covariant _ScanWindowPainter oldDelegate) {
    return oldDelegate.window != window;
  }
}
