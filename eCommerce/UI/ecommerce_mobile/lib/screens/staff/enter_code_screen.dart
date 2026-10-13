import 'package:ecommerce_mobile/models/ticket.dart';
import 'package:ecommerce_mobile/providers/ticket_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:ecommerce_mobile/utils/ticket_code.dart';
import 'package:ecommerce_mobile/widgets/ticket_verification_result.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class EnterCodeScreen extends StatefulWidget {
  const EnterCodeScreen({super.key});

  @override
  State<EnterCodeScreen> createState() => _EnterCodeScreenState();
}

class _EnterCodeScreenState extends State<EnterCodeScreen> {
  final _codeController = TextEditingController();
  bool _verifying = false;
  Ticket? _ticket;
  String? _error;
  String _errorTitle = 'Ticket rejected';

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  Future<void> _verify() async {
    final code = normalizeTicketCode(_codeController.text);
    if (!isWildNestTicketCode(code)) {
      setState(() {
        _verifying = false;
        _ticket = null;
        _errorTitle = 'Invalid code';
        _error = 'Enter a code like WN-A1B2C3.';
      });
      return;
    }

    setState(() {
      _verifying = true;
      _ticket = null;
      _error = null;
    });

    try {
      final ticket = await context.read<TicketProvider>().verify(
        code,
        method: TicketVerificationMethod.manualCode,
      );
      if (!mounted) return;
      setState(() {
        _ticket = ticket;
        _verifying = false;
      });
      await HapticFeedback.lightImpact();
    } on SessionExpiredException {
      if (mounted) setState(() => _verifying = false);
      return;
    } on ApiClientException catch (e) {
      if (!mounted) return;
      setState(() {
        _errorTitle = 'Ticket rejected';
        _error = e.message;
        _verifying = false;
      });
      await HapticFeedback.heavyImpact();
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _errorTitle = 'Ticket rejected';
        _error = 'Could not verify this ticket. Please try again.';
        _verifying = false;
      });
      await HapticFeedback.heavyImpact();
    }
  }

  void _checkAnother() {
    _codeController.clear();
    setState(() {
      _ticket = null;
      _error = null;
    });
  }

  @override
  Widget build(BuildContext context) {
    final hasResult = _verifying || _ticket != null || _error != null;
    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 4, 20, 24),
      keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
      children: [
        Text(
          'Enter code',
          style: GoogleFonts.playfairDisplay(
            fontSize: 30,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 6),
        const Text(
          'Type the code printed on the ticket.',
          style: TextStyle(color: AppColors.textMuted),
        ),
        const SizedBox(height: 18),
        TextField(
          controller: _codeController,
          textCapitalization: TextCapitalization.characters,
          textInputAction: TextInputAction.done,
          autocorrect: false,
          enableSuggestions: false,
          inputFormatters: [
            FilteringTextInputFormatter.allow(RegExp(r'[A-Za-z0-9-]')),
            LengthLimitingTextInputFormatter(9),
            _UpperCaseTextFormatter(),
          ],
          onSubmitted: (_) => _verifying ? null : _verify(),
          decoration: const InputDecoration(
            hintText: 'e.g. WN-A1B2C3',
            prefixIcon: Icon(Icons.confirmation_number_outlined),
          ),
        ),
        const SizedBox(height: 14),
        ElevatedButton(
          onPressed: _verifying ? null : _verify,
          child: Text(_verifying ? 'Checking...' : 'Verify'),
        ),
        if (hasResult) ...[
          const SizedBox(height: 16),
          TicketVerificationResult(
            loading: _verifying,
            ticket: _ticket,
            error: _error,
            errorTitle: _errorTitle,
            onNext: _verifying || (_ticket == null && _error == null)
                ? null
                : _checkAnother,
            nextLabel: 'Check another',
          ),
        ],
      ],
    );
  }
}

class _UpperCaseTextFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) {
    return newValue.copyWith(text: newValue.text.toUpperCase());
  }
}
