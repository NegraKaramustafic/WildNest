import 'dart:convert';

import 'package:ecommerce_mobile/models/user.dart';
import 'package:ecommerce_mobile/providers/user_provider.dart';
import 'package:ecommerce_mobile/theme/app_colors.dart';
import 'package:ecommerce_mobile/utils/profile_image.dart';
import 'package:ecommerce_mobile/utils/utils_widgets.dart';
import 'package:ecommerce_mobile/widgets/wildnest_header.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({super.key, required this.userId});

  final int userId;

  @override
  State<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  final _picker = ImagePicker();
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _email = TextEditingController();
  final _username = TextEditingController();
  final _phone = TextEditingController();

  bool _loading = true;
  bool _saving = false;
  bool _picking = false;
  bool _removeImage = false;
  String? _loadError;
  String? _formError;
  String? _firstNameError;
  String? _lastNameError;
  String? _emailError;
  String? _usernameError;
  String? _phoneError;
  String? _imageError;
  String? _existingBase64;
  String? _pickedBase64;
  Uint8List? _preview;

  static final _emailPattern = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
  static final _phonePattern = RegExp(r'^[0-9+\-() ]{6,20}$');

  bool get _busy => _saving || _picking;

  bool get _hasPicture =>
      !_removeImage &&
      (_preview != null ||
          (_existingBase64 != null && _existingBase64!.trim().isNotEmpty));

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  @override
  void dispose() {
    _firstName.dispose();
    _lastName.dispose();
    _email.dispose();
    _username.dispose();
    _phone.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _loadError = null;
    });
    final users = context.read<UserProvider>();
    try {
      final user = await users.getById(widget.userId);
      if (!mounted) return;
      _applyUser(user);
      setState(() => _loading = false);
    } catch (error) {
      if (!mounted || shouldIgnoreError(error)) return;
      setState(() {
        _loading = false;
        _loadError = error.toString();
      });
    }
  }

  void _applyUser(User user) {
    _firstName.text = user.firstName ?? '';
    _lastName.text = user.lastName ?? '';
    _email.text = user.email ?? '';
    _username.text = user.username ?? '';
    _phone.text = user.phoneNumber ?? '';
    _existingBase64 = user.profileImageBase64;
    _pickedBase64 = null;
    _removeImage = false;
    _preview = decodeProfileImage(user.profileImageBase64);
    _clearFieldErrors();
  }

  void _clearFieldErrors() {
    _formError = null;
    _firstNameError = null;
    _lastNameError = null;
    _emailError = null;
    _usernameError = null;
    _phoneError = null;
    _imageError = null;
  }

  Future<void> _pick(ImageSource source) async {
    if (_busy) return;
    setState(() {
      _picking = true;
      _imageError = null;
    });
    try {
      final file = await _picker.pickImage(
        source: source,
        maxWidth: 1600,
        maxHeight: 1600,
        imageQuality: 80,
        requestFullMetadata: false,
      );
      if (!mounted || file == null) return;
      final original = await file.readAsBytes();
      if (!mounted) return;
      final prepared = prepareProfileImage(original);
      if (prepared == null) {
        setState(() => _imageError = profileImageInvalidMessage);
        return;
      }
      setState(() {
        _preview = prepared;
        _pickedBase64 = base64Encode(prepared);
        _removeImage = false;
        _imageError = null;
      });
    } on PlatformException catch (error) {
      if (!mounted) return;
      setState(() => _imageError = _pickError(source, error.code));
    } catch (_) {
      if (!mounted) return;
      setState(
        () =>
            _imageError = 'The picture could not be opened. Please try again.',
      );
    } finally {
      if (mounted) {
        setState(() => _picking = false);
      } else {
        _picking = false;
      }
    }
  }

  String _pickError(ImageSource source, String code) {
    switch (code) {
      case 'camera_access_denied':
      case 'camera_access_restricted':
        return 'Camera access is required to take a profile picture. Allow it in your device settings.';
      case 'photo_access_denied':
      case 'photo_access_restricted':
        return 'Photo library access is required to choose a profile picture. Allow it in your device settings.';
      default:
        if (source == ImageSource.camera) {
          return 'The camera is not available on this device.';
        }
        return 'The picture could not be opened. Please try again.';
    }
  }

  Future<void> _confirmRemove() async {
    if (_busy || !_hasPicture) return;
    final remove = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Remove profile picture'),
        content: const Text('Remove this profile picture?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text(
              'Remove',
              style: TextStyle(color: AppColors.heart),
            ),
          ),
        ],
      ),
    );
    if (remove != true || !mounted) return;
    setState(() {
      _removeImage = true;
      _pickedBase64 = null;
      _preview = null;
      _imageError = null;
    });
  }

  Future<void> _save() async {
    if (_busy) return;
    setState(_clearFieldErrors);
    final valid = _formKey.currentState?.validate() ?? false;
    if (!valid) return;

    setState(() => _saving = true);
    final body = <String, dynamic>{
      'firstName': _firstName.text.trim(),
      'lastName': _lastName.text.trim(),
      'email': _email.text.trim(),
      'username': _username.text.trim(),
      'phoneNumber': _phone.text.trim().isEmpty ? null : _phone.text.trim(),
    };
    if (_removeImage) {
      body['profileImageBase64'] = '';
    } else if (_pickedBase64 != null) {
      body['profileImageBase64'] = _pickedBase64;
    } else if (_existingBase64 != null && _existingBase64!.trim().isNotEmpty) {
      body['profileImageBase64'] = _existingBase64;
    }

    final users = context.read<UserProvider>();
    try {
      await users.update(widget.userId, body);
      if (!mounted) return;
      Navigator.pop(context, true);
    } catch (error) {
      if (!mounted || shouldIgnoreError(error)) return;
      final message = error.toString().trim();
      _applyServerMessage(
        message.isEmpty
            ? 'Could not save your profile. Please try again.'
            : message,
      );
      setState(() => _saving = false);
      _formKey.currentState?.validate();
    }
  }

  void _applyServerMessage(String message) {
    final leftover = <String>[];
    for (final line in message.split('\n')) {
      final text = line.trim();
      if (text.isEmpty) continue;
      final lower = text.toLowerCase();
      if (lower.contains('first name')) {
        _firstNameError = text;
      } else if (lower.contains('last name')) {
        _lastNameError = text;
      } else if (lower.contains('email') || lower.contains('e-mail')) {
        _emailError = text;
      } else if (lower.contains('username')) {
        _usernameError = text;
      } else if (lower.contains('phone')) {
        _phoneError = text;
      } else if (lower.contains('image') ||
          lower.contains('png') ||
          lower.contains('jpeg')) {
        _imageError = text;
      } else {
        leftover.add(text);
      }
    }
    _formError = leftover.isEmpty ? null : leftover.join(' ');
  }

  void _clearServerError(void Function() clear) {
    clear();
    setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Column(
          children: [
            WildNestHeader(
              showBack: true,
              onBack: () => Navigator.pop(context, false),
            ),
            Expanded(child: _body()),
          ],
        ),
      ),
    );
  }

  Widget _body() {
    if (_loading) {
      return const Center(
        child: CircularProgressIndicator(color: AppColors.sage),
      );
    }
    if (_loadError != null) {
      return ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        children: [
          Text(
            'Edit profile',
            style: GoogleFonts.playfairDisplay(
              fontSize: 30,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 16),
          Text(_loadError!, style: const TextStyle(color: AppColors.heart)),
          const SizedBox(height: 16),
          OutlinedButton(onPressed: _load, child: const Text('Try again')),
        ],
      );
    }

    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
        children: [
          Text(
            'Edit profile',
            style: GoogleFonts.playfairDisplay(
              fontSize: 30,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 6),
          const Text(
            'Update your name, email, phone, username, and profile picture.',
            style: TextStyle(color: AppColors.textMuted),
          ),
          const SizedBox(height: 20),
          _picture(),
          const SizedBox(height: 20),
          TextFormField(
            controller: _firstName,
            textCapitalization: TextCapitalization.words,
            textInputAction: TextInputAction.next,
            enabled: !_saving,
            decoration: _decoration('First name', Icons.badge_outlined),
            validator: (value) => _limit(
              value,
              empty: 'First name is required.',
              max: 50,
              tooLong: 'First name cannot exceed 50 characters.',
              server: _firstNameError,
            ),
            onChanged: (_) {
              if (_firstNameError != null) {
                _clearServerError(() => _firstNameError = null);
              }
            },
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _lastName,
            textCapitalization: TextCapitalization.words,
            textInputAction: TextInputAction.next,
            enabled: !_saving,
            decoration: _decoration('Last name', Icons.badge_outlined),
            validator: (value) => _limit(
              value,
              empty: 'Last name is required.',
              max: 50,
              tooLong: 'Last name cannot exceed 50 characters.',
              server: _lastNameError,
            ),
            onChanged: (_) {
              if (_lastNameError != null) {
                _clearServerError(() => _lastNameError = null);
              }
            },
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _email,
            keyboardType: TextInputType.emailAddress,
            autocorrect: false,
            textInputAction: TextInputAction.next,
            enabled: !_saving,
            decoration: _decoration('Email', Icons.mail_outline),
            validator: (value) {
              final text = value?.trim() ?? '';
              if (text.isEmpty) return 'Email is required.';
              if (text.length > 100) {
                return 'Email cannot exceed 100 characters.';
              }
              if (!_emailPattern.hasMatch(text)) {
                return 'Email must be a valid email address.';
              }
              return _emailError;
            },
            onChanged: (_) {
              if (_emailError != null) {
                _clearServerError(() => _emailError = null);
              }
            },
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _username,
            autocorrect: false,
            textInputAction: TextInputAction.next,
            enabled: !_saving,
            decoration: _decoration('Username', Icons.person_outline),
            validator: (value) {
              final text = value?.trim() ?? '';
              if (text.isEmpty) return 'Username is required.';
              if (text.length < 3) {
                return 'Username must be at least 3 characters.';
              }
              if (text.length > 100) {
                return 'Username cannot exceed 100 characters.';
              }
              return _usernameError;
            },
            onChanged: (_) {
              if (_usernameError != null) {
                _clearServerError(() => _usernameError = null);
              }
            },
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: _phone,
            keyboardType: TextInputType.phone,
            textInputAction: TextInputAction.done,
            enabled: !_saving,
            decoration: _decoration(
              'Phone (optional)',
              Icons.phone_outlined,
            ).copyWith(helperText: 'Optional.', helperMaxLines: 2),
            validator: (value) {
              final text = value?.trim() ?? '';
              if (text.isEmpty) return _phoneError;
              if (!_phonePattern.hasMatch(text)) {
                return 'Phone number must be 6 to 20 characters and may contain only digits, spaces, and + - ( ).';
              }
              return _phoneError;
            },
            onChanged: (_) {
              if (_phoneError != null) {
                _clearServerError(() => _phoneError = null);
              }
            },
          ),
          if (_formError != null) ...[
            const SizedBox(height: 16),
            Text(_formError!, style: const TextStyle(color: AppColors.heart)),
          ],
          const SizedBox(height: 20),
          ElevatedButton(
            onPressed: _busy ? null : _save,
            child: _saving
                ? const SizedBox(
                    width: 22,
                    height: 22,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  )
                : const Text('Save profile'),
          ),
        ],
      ),
    );
  }

  Widget _picture() {
    final image = _preview;
    return Column(
      children: [
        Semantics(
          label: 'Profile picture.',
          child: Stack(
            alignment: Alignment.center,
            children: [
              CircleAvatar(
                radius: 52,
                backgroundColor: AppColors.mint,
                backgroundImage: image == null ? null : MemoryImage(image),
                child: image == null
                    ? const Icon(Icons.person, color: AppColors.sage, size: 48)
                    : null,
              ),
              if (_picking)
                const SizedBox(
                  width: 28,
                  height: 28,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: AppColors.sage,
                  ),
                ),
            ],
          ),
        ),
        const SizedBox(height: 8),
        const Text(
          'Use a PNG or JPEG picture of at most 512 KB.',
          textAlign: TextAlign.center,
          style: TextStyle(color: AppColors.textMuted, fontSize: 13),
        ),
        if (_imageError != null) ...[
          const SizedBox(height: 8),
          Text(
            _imageError!,
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.heart),
          ),
        ],
        const SizedBox(height: 12),
        OutlinedButton.icon(
          onPressed: _busy ? null : () => _pick(ImageSource.gallery),
          icon: const Icon(Icons.photo_library_outlined),
          label: const Text('Choose from gallery'),
        ),
        const SizedBox(height: 8),
        OutlinedButton.icon(
          onPressed: _busy ? null : () => _pick(ImageSource.camera),
          icon: const Icon(Icons.photo_camera_outlined),
          label: const Text('Take photo'),
        ),
        if (_hasPicture) ...[
          const SizedBox(height: 4),
          TextButton(
            onPressed: _busy ? null : _confirmRemove,
            child: const Text(
              'Remove picture',
              style: TextStyle(color: AppColors.heart),
            ),
          ),
        ],
      ],
    );
  }

  InputDecoration _decoration(String label, IconData icon) {
    return authInputDecoration(
      label: label,
      icon: icon,
    ).copyWith(errorMaxLines: 4);
  }

  String? _limit(
    String? value, {
    required String empty,
    required int max,
    required String tooLong,
    required String? server,
  }) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) return empty;
    if (text.length > max) return tooLong;
    return server;
  }
}
