import 'dart:typed_data';

import 'package:ecommerce_desktop/models/app_user.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/user_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/form_rules.dart';
import 'package:ecommerce_desktop/utils/image_bytes.dart';
import 'package:ecommerce_desktop/widgets/image_field.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class UserFormDialog extends StatefulWidget {
  const UserFormDialog({super.key, this.user, this.editingSelf = false});

  final AppUser? user;
  final bool editingSelf;

  @override
  State<UserFormDialog> createState() => _UserFormDialogState();
}

class _UserFormDialogState extends State<UserFormDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _firstName;
  late final TextEditingController _lastName;
  late final TextEditingController _email;
  late final TextEditingController _username;
  late final TextEditingController _phone;
  late final TextEditingController _currentPassword;
  late final TextEditingController _password;
  late final TextEditingController _confirm;
  late String _role;
  late bool _isActive;
  Uint8List? _pickedBytes;
  String? _pickedBase64;
  String? _existingImage;
  bool _clearImage = false;
  bool _saving = false;
  String? _formError;
  String? _imageError;

  bool get _isEdit => widget.user != null;

  @override
  void initState() {
    super.initState();
    final user = widget.user;
    _firstName = TextEditingController(text: user?.firstName ?? '');
    _lastName = TextEditingController(text: user?.lastName ?? '');
    _email = TextEditingController(text: user?.email ?? '');
    _username = TextEditingController(text: user?.username ?? '');
    _phone = TextEditingController(text: user?.phoneNumber ?? '');
    _currentPassword = TextEditingController();
    _password = TextEditingController();
    _confirm = TextEditingController();
    _role = user?.role == 'Admin' ? 'Admin' : 'Customer';
    _isActive = user?.isActive ?? true;
    _existingImage = user?.profileImageBase64;
  }

  @override
  void dispose() {
    _firstName.dispose();
    _lastName.dispose();
    _email.dispose();
    _username.dispose();
    _phone.dispose();
    _currentPassword.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _pickImage() async {
    final result = await pickPngOrJpeg();
    if (!mounted) return;
    setState(() {
      _imageError = result.error;
      if (result.image != null) {
        _pickedBytes = result.image!.bytes;
        _pickedBase64 = result.image!.base64;
        _clearImage = false;
      }
    });
  }

  Future<void> _save() async {
    final imageMessage = _pickedBytes == null ? null : imageValidationMessage(_pickedBytes!);
    setState(() => _imageError = imageMessage);
    if (_formKey.currentState?.validate() != true || imageMessage != null) return;

    setState(() {
      _saving = true;
      _formError = null;
    });
    final changingOwnPassword = widget.editingSelf &&
        (_password.text.trim().isNotEmpty || _confirm.text.trim().isNotEmpty);
    final body = <String, dynamic>{
      'firstName': _firstName.text.trim(),
      'lastName': _lastName.text.trim(),
      'email': _email.text.trim(),
      'username': _username.text.trim(),
      'phoneNumber': _phone.text.trim().isEmpty ? null : _phone.text.trim(),
      'isActive': widget.editingSelf ? true : _isActive,
      'role': _role,
      'profileImageBase64': _pickedBase64 ?? (_clearImage ? '' : _existingImage),
    };
    if (!widget.editingSelf && (_password.text.trim().isNotEmpty || !_isEdit)) {
      body['password'] = _password.text.trim();
      body['confirmPassword'] = _confirm.text.trim();
    }

    try {
      final provider = context.read<UserProvider>();
      final saved = _isEdit
          ? await provider.update(widget.user!.id, body)
          : await provider.insert(body);
      if (changingOwnPassword) {
        await provider.changePassword({
          'id': widget.user!.id,
          'password': _currentPassword.text,
          'newPassword': _password.text.trim(),
          'confirmNewPassword': _confirm.text.trim(),
        });
        if (!mounted) return;
        final auth = context.read<AuthProvider>();
        Navigator.pop(context);
        await auth.logout();
        return;
      }
      if (!mounted) return;
      Navigator.pop(context, saved);
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _formError = error.toString().replaceFirst('Bad state: ', '');
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 640, maxHeight: 760),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 16, 16, 18),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        _isEdit ? 'Edit user' : 'New user',
                        style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
                      ),
                    ),
                    IconButton(
                      tooltip: 'Close',
                      onPressed: _saving ? null : () => Navigator.pop(context),
                      icon: const Icon(Icons.close),
                    ),
                  ],
                ),
                Flexible(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.only(right: 8),
                    child: Column(
                      children: [
                        ImageField(
                          bytes: _clearImage ? null : _pickedBytes,
                          existingBase64: _clearImage ? null : _existingImage,
                          error: _imageError,
                          onPick: _pickImage,
                          onClear: (_pickedBytes != null || (_existingImage != null && !_clearImage))
                              ? () => setState(() {
                                  _pickedBytes = null;
                                  _pickedBase64 = null;
                                  _clearImage = true;
                                  _imageError = null;
                                })
                              : null,
                        ),
                        const SizedBox(height: 12),
                        Row(
                          children: [
                            Expanded(child: _field(_firstName, 'First name', (v) => requiredText(v, 'First name', max: 50))),
                            const SizedBox(width: 12),
                            Expanded(child: _field(_lastName, 'Last name', (v) => requiredText(v, 'Last name', max: 50))),
                          ],
                        ),
                        const SizedBox(height: 12),
                        _field(_email, 'Email', emailError),
                        const SizedBox(height: 12),
                        _field(_username, 'Username', (v) => requiredText(v, 'Username', min: 3, max: 100)),
                        const SizedBox(height: 12),
                        _field(_phone, 'Phone', phoneError, required: false),
                        const SizedBox(height: 12),
                        DropdownButtonFormField<String>(
                          key: ValueKey('role-$_role'),
                          initialValue: _role,
                          isExpanded: true,
                          decoration: const InputDecoration(labelText: 'Role', isDense: true),
                          items: const [
                            DropdownMenuItem(value: 'Customer', child: Text('Customer')),
                            DropdownMenuItem(value: 'Admin', child: Text('Admin')),
                          ],
                          onChanged: widget.editingSelf
                              ? null
                              : (value) {
                                  if (value != null) setState(() => _role = value);
                                },
                        ),
                        if (widget.editingSelf)
                          const Align(
                            alignment: Alignment.centerLeft,
                            child: Padding(
                              padding: EdgeInsets.only(top: 4),
                              child: Text(
                                'You cannot remove your own administrator role.',
                                style: TextStyle(color: AppColors.textMuted, fontSize: 12),
                              ),
                            ),
                          ),
                        const SizedBox(height: 12),
                        if (widget.editingSelf) ...[
                          _field(
                            _currentPassword,
                            'Current password',
                            (value) {
                              final changing = _password.text.trim().isNotEmpty || _confirm.text.trim().isNotEmpty;
                              if (!changing) return null;
                              if (value == null || value.trim().isEmpty) {
                                return 'Current password is required.';
                              }
                              return null;
                            },
                            required: false,
                            markOptional: false,
                            obscure: true,
                            helper: 'Required when you set a new password.',
                          ),
                          const SizedBox(height: 12),
                        ],
                        _field(
                          _password,
                          _isEdit ? 'New password' : 'Password',
                          (value) => passwordError(
                            required: !_isEdit,
                            password: value,
                            confirm: _confirm.text,
                          ),
                          obscure: true,
                          helper: _isEdit
                              ? (widget.editingSelf
                                  ? 'Leave blank to keep the current password. Your current password is required to set a new one.'
                                  : 'Leave blank to keep the current password.')
                              : null,
                        ),
                        const SizedBox(height: 12),
                        _field(
                          _confirm,
                          'Confirm password',
                          (value) => confirmPasswordError(
                            required: !_isEdit,
                            password: _password.text,
                            confirm: value,
                          ),
                          obscure: true,
                        ),
                        const SizedBox(height: 4),
                        widget.editingSelf
                            ? const Tooltip(
                                message: 'You cannot block your own account.',
                                child: SwitchListTile(
                                  contentPadding: EdgeInsets.zero,
                                  title: Text('Active'),
                                  subtitle: Text('You cannot block your own account.'),
                                  value: true,
                                  onChanged: null,
                                ),
                              )
                            : SwitchListTile(
                                contentPadding: EdgeInsets.zero,
                                title: const Text('Active'),
                                subtitle: const Text('Blocked users cannot sign in.'),
                                value: _isActive,
                                onChanged: (value) => setState(() => _isActive = value),
                              ),
                        if (_formError != null) ...[
                          const SizedBox(height: 8),
                          Text(_formError!, style: const TextStyle(color: AppColors.danger)),
                        ],
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(
                      onPressed: _saving ? null : () => Navigator.pop(context),
                      child: const Text('Cancel'),
                    ),
                    const SizedBox(width: 8),
                    FilledButton(
                      onPressed: _saving ? null : _save,
                      child: Text(_saving ? 'Saving...' : 'Save'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _field(
    TextEditingController controller,
    String label,
    String? Function(String?) validator, {
    bool required = true,
    bool markOptional = true,
    bool obscure = false,
    String? helper,
  }) {
    return TextFormField(
      controller: controller,
      obscureText: obscure,
      autovalidateMode: AutovalidateMode.onUserInteraction,
      decoration: InputDecoration(
        labelText: required || !markOptional ? label : '$label (optional)',
        isDense: true,
        helperText: helper,
        helperMaxLines: 2,
        errorMaxLines: 3,
      ),
      validator: validator,
    );
  }
}
