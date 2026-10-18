import 'dart:async';

import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/screens/auth/reset_password_screen.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  bool _sent = false;
  bool _loading = false;
  int _cooldown = 0;
  Timer? _timer;
  String? _emailError;

  @override
  void dispose() {
    _timer?.cancel();
    _email.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _emailError = null);
    if (!_sent && !_formKey.currentState!.validate()) {
      return;
    }
    final email = _email.text.trim();
    if (email.isEmpty || !email.contains('@')) {
      return;
    }
    setState(() => _loading = true);
    try {
      final seconds = await context.read<AuthProvider>().requestPasswordReset(email);
      if (!mounted) return;
      setState(() => _sent = true);
      _startCooldown(seconds);
    } on ApiClientException catch (e) {
      if (!mounted) return;
      if (_formKey.currentState != null) {
        setState(() => _emailError = e.message);
        _formKey.currentState!.validate();
      } else {
        showAppError(context, e);
      }
    } catch (e) {
      if (!mounted) return;
      showAppError(context, e);
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  void _startCooldown(int seconds) {
    _timer?.cancel();
    setState(() => _cooldown = seconds < 0 ? 0 : seconds);
    if (_cooldown == 0) return;
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) {
        timer.cancel();
        return;
      }
      setState(() {
        _cooldown -= 1;
        if (_cooldown <= 0) {
          _cooldown = 0;
          timer.cancel();
        }
      });
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Forgot password')),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
          child: _sent ? _confirmation() : _form(),
        ),
      ),
    );
  }

  Widget _form() {
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            'Reset your password',
            style: GoogleFonts.playfairDisplay(
              fontSize: 28,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 8),
          const Text(
            'Enter the email linked to your WildNest account and we will send a reset code.',
            style: TextStyle(color: AppColors.textMuted, height: 1.4),
          ),
          const SizedBox(height: 24),
          TextFormField(
            controller: _email,
            keyboardType: TextInputType.emailAddress,
            enabled: !_loading,
            decoration: authInputDecoration(
              label: 'Email',
              icon: Icons.mail_outline,
            ),
            onChanged: (_) {
              if (_emailError != null) {
                setState(() => _emailError = null);
              }
            },
            validator: (v) {
              if (v == null || v.trim().isEmpty) return mandatoryField;
              if (!v.contains('@')) return 'Enter a valid email';
              return _emailError;
            },
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            onPressed: _loading ? null : _submit,
            child: _loading
                ? const SizedBox(
                    width: 22,
                    height: 22,
                    child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                  )
                : const Text('Send code'),
          ),
        ],
      ),
    );
  }

  Widget _confirmation() {
    final waiting = _cooldown > 0 || _loading;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SizedBox(height: 24),
        Container(
          width: 72,
          height: 72,
          decoration: const BoxDecoration(
            color: AppColors.mint,
            shape: BoxShape.circle,
          ),
          child: const Icon(Icons.mark_email_read_outlined, color: AppColors.sage),
        ),
        const SizedBox(height: 20),
        Text(
          'Check your inbox',
          style: GoogleFonts.playfairDisplay(
            fontSize: 26,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          'If an account exists for ${_email.text.trim()}, you will receive a reset code shortly.',
          style: const TextStyle(color: AppColors.textMuted, height: 1.4),
        ),
        const SizedBox(height: 28),
        ElevatedButton(
          onPressed: () {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) => ResetPasswordScreen(
                  email: _email.text.trim(),
                  cooldownSeconds: _cooldown,
                ),
              ),
            );
          },
          child: const Text('I have a reset code'),
        ),
        const SizedBox(height: 12),
        OutlinedButton(
          onPressed: waiting ? null : _submit,
          child: Text(_cooldown > 0 ? 'Resend code (${_cooldown}s)' : 'Resend code'),
        ),
        const SizedBox(height: 12),
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Back to sign in'),
        ),
      ],
    );
  }
}
