import 'dart:async';

import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _emailController = TextEditingController();
  final _codeController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();
  final _emailFormKey = GlobalKey<FormState>();
  final _resetFormKey = GlobalKey<FormState>();

  bool _codeStep = false;
  bool _done = false;
  bool _loading = false;
  bool _obscure = true;
  int _cooldown = 0;
  Timer? _timer;
  String? _banner;

  @override
  void dispose() {
    _timer?.cancel();
    _emailController.dispose();
    _codeController.dispose();
    _passwordController.dispose();
    _confirmController.dispose();
    super.dispose();
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

  Future<void> _sendCode() async {
    setState(() => _banner = null);
    if (!_codeStep && !_emailFormKey.currentState!.validate()) {
      return;
    }
    final email = _emailController.text.trim();
    if (email.isEmpty || !email.contains('@')) {
      setState(() => _banner = 'Enter a valid email.');
      return;
    }
    setState(() => _loading = true);
    try {
      final seconds = await context.read<AuthProvider>().requestPasswordReset(
        _emailController.text.trim(),
      );
      if (!mounted) return;
      setState(() => _codeStep = true);
      _startCooldown(seconds);
    } on ApiClientException catch (e) {
      if (!mounted) return;
      setState(() => _banner = e.message);
    } catch (e) {
      if (!mounted) return;
      setState(() => _banner = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _reset() async {
    setState(() => _banner = null);
    if (!_resetFormKey.currentState!.validate()) {
      return;
    }
    setState(() => _loading = true);
    try {
      await context.read<AuthProvider>().resetPassword(
        email: _emailController.text.trim(),
        code: _codeController.text.trim(),
        newPassword: _passwordController.text,
        confirmNewPassword: _confirmController.text,
      );
      if (!mounted) return;
      setState(() => _done = true);
    } on ApiClientException catch (e) {
      if (!mounted) return;
      setState(() => _banner = e.message);
    } catch (e) {
      if (!mounted) return;
      setState(() => _banner = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 420),
          child: Card(
            elevation: 0,
            color: AppColors.surface,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(16),
              side: const BorderSide(color: AppColors.border),
            ),
            child: Padding(
              padding: const EdgeInsets.fromLTRB(28, 32, 28, 28),
              child: _done ? _success() : (_codeStep ? _resetForm() : _emailForm()),
            ),
          ),
        ),
      ),
    );
  }

  Widget _emailForm() {
    return Form(
      key: _emailFormKey,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Forgot password',
            style: TextStyle(fontSize: 26, fontWeight: FontWeight.w700, color: AppColors.text),
          ),
          const SizedBox(height: 6),
          const Text(
            'Enter the email on your account. If it exists, we will send a reset code.',
            style: TextStyle(color: AppColors.textMuted, height: 1.4),
          ),
          const SizedBox(height: 24),
          TextFormField(
            controller: _emailController,
            keyboardType: TextInputType.emailAddress,
            enabled: !_loading,
            decoration: const InputDecoration(labelText: 'Email'),
            validator: (value) {
              final text = value?.trim() ?? '';
              if (text.isEmpty) return 'Email is required.';
              if (!text.contains('@')) return 'Enter a valid email.';
              return null;
            },
          ),
          if (_banner != null) ...[
            const SizedBox(height: 16),
            _Banner(message: _banner!),
          ],
          const SizedBox(height: 20),
          FilledButton(
            onPressed: _loading ? null : _sendCode,
            child: Text(_loading ? 'Sending...' : 'Send code'),
          ),
          const SizedBox(height: 8),
          TextButton(
            onPressed: _loading ? null : () => Navigator.pop(context),
            child: const Text('Back to sign in'),
          ),
        ],
      ),
    );
  }

  Widget _resetForm() {
    final waiting = _cooldown > 0 || _loading;
    return Form(
      key: _resetFormKey,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'Reset password',
            style: TextStyle(fontSize: 26, fontWeight: FontWeight.w700, color: AppColors.text),
          ),
          const SizedBox(height: 6),
          Text(
            'Enter the code sent to ${_emailController.text.trim()} and choose a new password.',
            style: const TextStyle(color: AppColors.textMuted, height: 1.4),
          ),
          const SizedBox(height: 24),
          TextFormField(
            controller: _codeController,
            enabled: !_loading,
            keyboardType: TextInputType.number,
            inputFormatters: [
              FilteringTextInputFormatter.digitsOnly,
              LengthLimitingTextInputFormatter(6),
            ],
            decoration: const InputDecoration(labelText: 'Reset code'),
            validator: (value) {
              final text = value?.trim() ?? '';
              if (text.isEmpty) return 'Reset code is required.';
              if (!RegExp(r'^\d{6}$').hasMatch(text)) return 'Enter the 6-digit code.';
              return null;
            },
          ),
          const SizedBox(height: 14),
          TextFormField(
            controller: _passwordController,
            obscureText: _obscure,
            enabled: !_loading,
            decoration: InputDecoration(
              labelText: 'New password',
              suffixIcon: IconButton(
                onPressed: () => setState(() => _obscure = !_obscure),
                icon: Icon(_obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined),
              ),
            ),
            validator: (value) {
              if (value == null || value.isEmpty) return 'Password is required.';
              if (value.length < 6) return 'Password must be at least 6 characters.';
              return null;
            },
          ),
          const SizedBox(height: 14),
          TextFormField(
            controller: _confirmController,
            obscureText: _obscure,
            enabled: !_loading,
            decoration: const InputDecoration(labelText: 'Confirm new password'),
            validator: (value) {
              if (value == null || value.isEmpty) return 'Confirm the new password.';
              if (value != _passwordController.text) return 'Passwords do not match.';
              return null;
            },
          ),
          if (_banner != null) ...[
            const SizedBox(height: 16),
            _Banner(message: _banner!),
          ],
          const SizedBox(height: 20),
          FilledButton(
            onPressed: _loading ? null : _reset,
            child: Text(_loading ? 'Saving...' : 'Reset password'),
          ),
          const SizedBox(height: 8),
          OutlinedButton(
            onPressed: waiting ? null : _sendCode,
            child: Text(_cooldown > 0 ? 'Resend code (${_cooldown}s)' : 'Resend code'),
          ),
          TextButton(
            onPressed: _loading ? null : () => Navigator.pop(context),
            child: const Text('Back to sign in'),
          ),
        ],
      ),
    );
  }

  Widget _success() {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Text(
          'Password updated',
          style: TextStyle(fontSize: 26, fontWeight: FontWeight.w700, color: AppColors.text),
        ),
        const SizedBox(height: 8),
        const Text(
          'You can now sign in with your new password.',
          style: TextStyle(color: AppColors.textMuted, height: 1.4),
        ),
        const SizedBox(height: 20),
        FilledButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Back to sign in'),
        ),
      ],
    );
  }
}

class _Banner extends StatelessWidget {
  const _Banner({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: AppColors.errorBg,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: AppColors.errorBorder),
      ),
      child: Text(
        message,
        style: const TextStyle(color: AppColors.errorText, height: 1.35),
      ),
    );
  }
}
