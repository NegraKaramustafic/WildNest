import 'dart:async';

import 'package:ecommerce_mobile/providers/auth_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/api_client_exception.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

class ResetPasswordScreen extends StatefulWidget {
  const ResetPasswordScreen({super.key, this.email, this.cooldownSeconds = 0});

  final String? email;
  final int cooldownSeconds;

  @override
  State<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends State<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _code = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  bool _done = false;
  bool _loading = false;
  int _cooldown = 0;
  Timer? _timer;
  String? _codeError;
  String? _passwordError;

  @override
  void initState() {
    super.initState();
    if (widget.email != null) {
      _email.text = widget.email!;
    }
    _startCooldown(widget.cooldownSeconds);
  }

  @override
  void dispose() {
    _timer?.cancel();
    _email.dispose();
    _code.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  void _startCooldown(int seconds) {
    _timer?.cancel();
    _cooldown = seconds < 0 ? 0 : seconds;
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

  Future<void> _submit() async {
    setState(() {
      _codeError = null;
      _passwordError = null;
    });
    if (!_formKey.currentState!.validate()) {
      return;
    }
    setState(() => _loading = true);
    try {
      await context.read<AuthProvider>().resetPassword(
        email: _email.text.trim(),
        code: _code.text.trim(),
        newPassword: _password.text,
        confirmNewPassword: _confirm.text,
      );
      if (!mounted) return;
      setState(() => _done = true);
    } on ApiClientException catch (e) {
      if (!mounted) return;
      final message = e.message.toLowerCase();
      setState(() {
        if (message.contains('password')) {
          _passwordError = e.message;
        } else {
          _codeError = e.message;
        }
      });
      _formKey.currentState!.validate();
    } catch (e) {
      if (!mounted) return;
      showAppError(context, e);
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _resend() async {
    final email = _email.text.trim();
    if (email.isEmpty || !email.contains('@')) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Enter the email for your account.')),
      );
      return;
    }
    setState(() => _loading = true);
    try {
      final seconds = await context.read<AuthProvider>().requestPasswordReset(
        _email.text.trim(),
      );
      if (!mounted) return;
      _startCooldown(seconds);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('If an account exists for that email, a new code has been sent.')),
      );
    } on ApiClientException catch (e) {
      if (!mounted) return;
      setState(() => _codeError = e.message);
      _formKey.currentState!.validate();
    } catch (e) {
      if (!mounted) return;
      showAppError(context, e);
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Reset password')),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 8, 24, 24),
          child: _done ? _success() : _form(),
        ),
      ),
    );
  }

  Widget _form() {
    final waiting = _cooldown > 0 || _loading;
    return Form(
      key: _formKey,
      child: ListView(
        children: [
          Text(
            'Choose a new password',
            style: GoogleFonts.playfairDisplay(
              fontSize: 28,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            _email.text.trim().isEmpty
                ? 'Enter the code from your email and pick a new password.'
                : 'Enter the code sent to ${_email.text.trim()} and pick a new password.',
            style: const TextStyle(color: AppColors.textMuted, height: 1.4),
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
            validator: (v) {
              if (v == null || v.trim().isEmpty) return mandatoryField;
              if (!v.contains('@')) return 'Enter a valid email';
              return null;
            },
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _code,
            enabled: !_loading,
            keyboardType: TextInputType.number,
            inputFormatters: [
              FilteringTextInputFormatter.digitsOnly,
              LengthLimitingTextInputFormatter(6),
            ],
            decoration: authInputDecoration(
              label: 'Reset code',
              icon: Icons.pin_outlined,
            ),
            onChanged: (_) {
              if (_codeError != null) setState(() => _codeError = null);
            },
            validator: (v) {
              if (v == null || v.trim().isEmpty) return mandatoryField;
              if (!RegExp(r'^\d{6}$').hasMatch(v.trim())) {
                return 'Enter the 6-digit code';
              }
              return _codeError;
            },
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _password,
            obscureText: true,
            enabled: !_loading,
            decoration: authInputDecoration(
              label: 'New password',
              icon: Icons.lock_outline,
            ),
            onChanged: (_) {
              if (_passwordError != null) setState(() => _passwordError = null);
            },
            validator: (v) {
              if (v == null || v.isEmpty) return mandatoryField;
              if (v.length < 6) return 'Password must be at least 6 characters';
              return _passwordError;
            },
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _confirm,
            obscureText: true,
            enabled: !_loading,
            decoration: authInputDecoration(
              label: 'Confirm new password',
              icon: Icons.lock_outline,
            ),
            validator: (v) {
              if (v == null || v.isEmpty) return mandatoryField;
              if (v != _password.text) return 'Passwords do not match';
              return null;
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
                : const Text('Reset password'),
          ),
          const SizedBox(height: 12),
          OutlinedButton(
            onPressed: waiting ? null : _resend,
            child: Text(_cooldown > 0 ? 'Resend code (${_cooldown}s)' : 'Resend code'),
          ),
        ],
      ),
    );
  }

  Widget _success() {
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
          child: const Icon(Icons.check_rounded, color: AppColors.sage, size: 36),
        ),
        const SizedBox(height: 20),
        Text(
          'Password updated',
          style: GoogleFonts.playfairDisplay(
            fontSize: 26,
            fontWeight: FontWeight.w600,
          ),
        ),
        const SizedBox(height: 8),
        const Text(
          'You can now sign in with your new password.',
          style: TextStyle(color: AppColors.textMuted, height: 1.4),
        ),
        const SizedBox(height: 28),
        ElevatedButton(
          onPressed: () {
            Navigator.of(context).popUntil((route) => route.isFirst);
          },
          child: const Text('Back to sign in'),
        ),
      ],
    );
  }
}
