import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscure = true;
  bool _submitting = false;
  String? _error;

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final username = _usernameController.text.trim();
    final password = _passwordController.text;
    if (username.isEmpty || password.isEmpty) {
      setState(() {
        _error = 'Unesite korisničko ime i lozinku.';
      });
      return;
    }

    setState(() {
      _submitting = true;
      _error = null;
    });

    final auth = context.read<AuthProvider>();
    auth.clearSessionNotice();
    try {
      await auth.login(username, password);
    } on ApiClientException catch (e) {
      if (!mounted) return;
      setState(() => _error = e.message);
    } catch (e) {
      if (!mounted) return;
      setState(() => _error = e.toString().replaceFirst('Bad state: ', ''));
    } finally {
      if (mounted) {
        setState(() => _submitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final notice = context.watch<AuthProvider>().sessionNotice;

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
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Row(
                    children: [
                      _LogoMark(),
                      SizedBox(width: 12),
                      Text(
                        'WildNest',
                        style: TextStyle(
                          fontSize: 26,
                          fontWeight: FontWeight.w700,
                          color: AppColors.text,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  const Text(
                    'Admin panel',
                    style: TextStyle(color: AppColors.textMuted, fontSize: 14),
                  ),
                  const SizedBox(height: 24),
                  if (notice != null) ...[
                    _MessageBanner(message: notice, tone: _BannerTone.warning),
                    const SizedBox(height: 16),
                  ],
                  TextField(
                    controller: _usernameController,
                    textInputAction: TextInputAction.next,
                    decoration: const InputDecoration(
                      labelText: 'Korisničko ime',
                    ),
                    onSubmitted: (_) => _submit(),
                  ),
                  const SizedBox(height: 14),
                  TextField(
                    controller: _passwordController,
                    obscureText: _obscure,
                    decoration: InputDecoration(
                      labelText: 'Lozinka',
                      suffixIcon: IconButton(
                        onPressed: () => setState(() => _obscure = !_obscure),
                        icon: Icon(
                          _obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined,
                        ),
                      ),
                    ),
                    onSubmitted: (_) => _submit(),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 16),
                    _MessageBanner(message: _error!, tone: _BannerTone.error),
                  ],
                  const SizedBox(height: 20),
                  FilledButton(
                    onPressed: _submitting ? null : _submit,
                    child: Text(_submitting ? 'Prijava...' : 'Prijava'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _LogoMark extends StatelessWidget {
  const _LogoMark();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 36,
      height: 36,
      decoration: BoxDecoration(
        color: AppColors.green,
        borderRadius: BorderRadius.circular(10),
      ),
      child: const Icon(Icons.eco, color: Colors.white, size: 20),
    );
  }
}

enum _BannerTone { error, warning }

class _MessageBanner extends StatelessWidget {
  const _MessageBanner({required this.message, required this.tone});

  final String message;
  final _BannerTone tone;

  @override
  Widget build(BuildContext context) {
    final isError = tone == _BannerTone.error;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: isError ? AppColors.errorBg : AppColors.warningBg,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(
          color: isError ? AppColors.errorBorder : AppColors.warningBorder,
        ),
      ),
      child: Text(
        message,
        style: TextStyle(
          color: isError ? AppColors.errorText : AppColors.warningText,
          height: 1.35,
        ),
      ),
    );
  }
}
