import 'dart:async';
import 'dart:typed_data';

import 'package:ecommerce_desktop/models/zoo_partner.dart';
import 'package:ecommerce_desktop/providers/zoo_partner_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/form_rules.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/utils/image_bytes.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:ecommerce_desktop/widgets/image_field.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ZooPartnersScreen extends StatefulWidget {
  const ZooPartnersScreen({super.key});

  @override
  State<ZooPartnersScreen> createState() => _ZooPartnersScreenState();
}

class _ZooPartnersScreenState extends State<ZooPartnersScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  String? _status;
  bool _loading = true;
  String? _error;
  List<ZooPartner> _partners = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final requestId = ++_requestId;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await context.read<ZooPartnerProvider>().get(
        filter: {
          'FTS': _searchController.text.trim(),
          'IsActive': _status == null ? null : _status == 'active',
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      final items = result.items ?? [];
      final total = result.totalCount ?? items.length;
      if (items.isEmpty && total > 0 && _page > 1) {
        setState(() => _page -= 1);
        await _load();
        return;
      }
      setState(() {
        _partners = items;
        _total = total;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = friendlyError(error);
        _loading = false;
      });
    }
  }

  void _apply(VoidCallback change) {
    setState(() {
      change();
      _page = 1;
    });
    _load();
  }

  Future<void> _edit({ZooPartner? partner}) async {
    final saved = await showDialog<ZooPartner>(
      context: context,
      builder: (_) => _PartnerDialog(partner: partner),
    );
    if (saved == null || !mounted) return;
    _toast(partner == null ? 'Zoo partner added.' : 'Zoo partner updated.');
    if (partner == null) _page = 1;
    await _load();
  }

  Future<void> _delete(ZooPartner partner) async {
    final confirmed = await confirmAdminAction(
      context: context,
      title: 'Delete zoo partner',
      message: 'Delete ${partner.name}?',
      confirmLabel: 'Delete',
      destructive: true,
    );
    if (!confirmed || !mounted) return;
    try {
      await context.read<ZooPartnerProvider>().remove(partner.id);
      if (!mounted) return;
      _toast('Zoo partner deleted.');
      await _load();
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      _toast(friendlyError(error));
    }
  }

  Future<void> _details(ZooPartner partner) async {
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(partner.name),
        content: SizedBox(
          width: 480,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Align(alignment: Alignment.centerLeft, child: EntityThumbnail(url: partner.imageUrl)),
              const SizedBox(height: 12),
              DetailLine(label: 'Location', value: _text(partner.location)),
              DetailLine(label: 'Description', value: _text(partner.description)),
              DetailLine(label: 'Goal', value: formatKm(partner.goalAmount)),
              DetailLine(label: 'Raised', value: formatKm(partner.currentAmount)),
              DetailLine(label: 'Donations', value: '${partner.donationCount}'),
              DetailLine(label: 'Status', value: partner.isActive ? 'Active' : 'Inactive'),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Close')),
        ],
      ),
    );
  }

  void _toast(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    return AdminScreenFrame(
      title: 'Zoo partners',
      subtitle: 'Small zoos that receive donations. Raised amounts change only when a donation is paid.',
      filters: Wrap(
        spacing: 12,
        runSpacing: 12,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          SizedBox(
            width: 320,
            child: TextField(
              controller: _searchController,
              onChanged: (_) {
                _debounce?.cancel();
                _debounce = Timer(const Duration(milliseconds: 300), () {
                  if (!mounted) return;
                  setState(() => _page = 1);
                  _load();
                });
              },
              decoration: const InputDecoration(
                hintText: 'Search by name or location...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          SizedBox(width: 180, child: _statusFilter()),
          FilledButton.icon(
            onPressed: () => _edit(),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Add partner'),
          ),
        ],
      ),
      body: _body(),
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) return ListError(message: _error!, onRetry: _load);
    const headerStyle = TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textMuted);
    return AdminTableCard(
      empty: 'No zoo partners to show.',
      pager: ListPager(
        page: _page,
        pageSize: _pageSize,
        total: _total,
        onPage: (page) {
          setState(() => _page = page);
          _load();
        },
      ),
      header: const Padding(
        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            SizedBox(width: 54),
            Expanded(flex: 3, child: Text('Name', style: headerStyle)),
            Expanded(flex: 2, child: Text('Location', style: headerStyle)),
            Expanded(flex: 2, child: Text('Raised', style: headerStyle)),
            Expanded(flex: 2, child: Text('Goal', style: headerStyle)),
            Expanded(flex: 2, child: Text('Status', style: headerStyle)),
            SizedBox(width: 136, child: Text('Actions', style: headerStyle)),
          ],
        ),
      ),
      rows: [
        for (final partner in _partners)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                EntityThumbnail(url: partner.imageUrl),
                const SizedBox(width: 12),
                Expanded(flex: 3, child: EllipsisText(partner.name, style: const TextStyle(fontWeight: FontWeight.w600))),
                Expanded(flex: 2, child: EllipsisText(_text(partner.location))),
                Expanded(flex: 2, child: EllipsisText(formatKm(partner.currentAmount))),
                Expanded(flex: 2, child: EllipsisText(formatKm(partner.goalAmount))),
                Expanded(
                  flex: 2,
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: labeledStatusChip(
                      label: partner.isActive ? 'Active' : 'Inactive',
                      tone: partner.isActive ? 'success' : 'neutral',
                    ),
                  ),
                ),
                SizedBox(
                  width: 136,
                  child: Row(
                    children: [
                      IconActionButton(
                        icon: Icons.visibility_outlined,
                        color: AppColors.text,
                        tooltip: 'View details',
                        enabled: true,
                        onPressed: () => _details(partner),
                      ),
                      IconActionButton(
                        icon: Icons.edit_outlined,
                        color: AppColors.text,
                        tooltip: 'Edit',
                        enabled: true,
                        onPressed: () => _edit(partner: partner),
                      ),
                      IconActionButton(
                        icon: Icons.delete_outline,
                        color: AppColors.danger,
                        tooltip: partner.isInUse
                            ? 'This zoo partner has donations and cannot be deleted. Deactivate it instead.'
                            : 'Delete',
                        enabled: !partner.isInUse,
                        onPressed: () => _delete(partner),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _statusFilter() {
    return DropdownButtonFormField<String?>(
      key: ValueKey('partner-status-$_status'),
      initialValue: _status,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Status', isDense: true),
      items: const [
        DropdownMenuItem(value: null, child: Text('All statuses', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'active', child: Text('Active', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'inactive', child: Text('Inactive', overflow: TextOverflow.ellipsis)),
      ],
      onChanged: (value) => _apply(() => _status = value),
    );
  }
}

class _PartnerDialog extends StatefulWidget {
  const _PartnerDialog({this.partner});

  final ZooPartner? partner;

  @override
  State<_PartnerDialog> createState() => _PartnerDialogState();
}

class _PartnerDialogState extends State<_PartnerDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _location;
  late final TextEditingController _description;
  late final TextEditingController _goal;
  late bool _isActive;
  Uint8List? _pickedBytes;
  String? _pickedBase64;
  String? _pickedType;
  String? _existingUrl;
  bool _clearImage = false;
  bool _saving = false;
  String? _formError;
  String? _imageError;

  bool get _isEdit => widget.partner != null;

  @override
  void initState() {
    super.initState();
    final partner = widget.partner;
    _name = TextEditingController(text: partner?.name ?? '');
    _location = TextEditingController(text: partner?.location ?? '');
    _description = TextEditingController(text: partner?.description ?? '');
    _goal = TextEditingController(
      text: partner == null ? '' : partner.goalAmount.toStringAsFixed(partner.goalAmount == partner.goalAmount.roundToDouble() ? 0 : 2),
    );
    _isActive = partner?.isActive ?? true;
    _existingUrl = partner?.imageUrl;
  }

  @override
  void dispose() {
    _name.dispose();
    _location.dispose();
    _description.dispose();
    _goal.dispose();
    super.dispose();
  }

  Future<void> _pick() async {
    final result = await pickPngOrJpeg();
    if (!mounted) return;
    setState(() {
      _imageError = result.error;
      if (result.image != null) {
        _pickedBytes = result.image!.bytes;
        _pickedBase64 = result.image!.base64;
        _pickedType = result.image!.contentType;
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
    final body = <String, dynamic>{
      'name': _name.text.trim(),
      'location': _location.text.trim(),
      'description': _description.text.trim().isEmpty ? null : _description.text.trim(),
      'goalAmount': double.parse(_goal.text.trim()),
      'isActive': _isActive,
      'imageUrl': _clearImage ? '' : _existingUrl,
      'imageBase64': _pickedBase64,
      'imageContentType': _pickedType,
    };
    try {
      final provider = context.read<ZooPartnerProvider>();
      final saved = _isEdit
          ? await provider.update(widget.partner!.id, body)
          : await provider.insert(body);
      if (!mounted) return;
      Navigator.pop(context, saved);
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _formError = friendlyError(error);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 560, maxHeight: 760),
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
                        _isEdit ? 'Edit zoo partner' : 'New zoo partner',
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
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        ImageField(
                          bytes: _clearImage ? null : _pickedBytes,
                          existingUrl: _clearImage ? null : _existingUrl,
                          error: _imageError,
                          onPick: _pick,
                          onClear: (_pickedBytes != null || (_existingUrl != null && !_clearImage))
                              ? () => setState(() {
                                  _pickedBytes = null;
                                  _pickedBase64 = null;
                                  _pickedType = null;
                                  _clearImage = true;
                                  _imageError = null;
                                })
                              : null,
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _name,
                          autovalidateMode: AutovalidateMode.onUserInteraction,
                          decoration: const InputDecoration(labelText: 'Name', isDense: true, errorMaxLines: 2),
                          validator: (value) => requiredText(value, 'Name', max: 100),
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _location,
                          autovalidateMode: AutovalidateMode.onUserInteraction,
                          decoration: const InputDecoration(
                            labelText: 'Location',
                            isDense: true,
                            helperText: 'City or region shown with the partner.',
                            helperMaxLines: 2,
                            errorMaxLines: 2,
                          ),
                          validator: (value) => requiredText(value, 'Location', max: 200),
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _description,
                          maxLines: 3,
                          maxLength: 1000,
                          autovalidateMode: AutovalidateMode.onUserInteraction,
                          decoration: const InputDecoration(
                            labelText: 'Description (optional)',
                            alignLabelWithHint: true,
                            errorMaxLines: 2,
                          ),
                          validator: (value) => optionalText(value, 'Description', 1000),
                        ),
                        const SizedBox(height: 12),
                        TextFormField(
                          controller: _goal,
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          autovalidateMode: AutovalidateMode.onUserInteraction,
                          decoration: const InputDecoration(
                            labelText: 'Goal amount (KM)',
                            isDense: true,
                            errorMaxLines: 2,
                          ),
                          validator: goalAmountError,
                        ),
                        if (_isEdit) ...[
                          const SizedBox(height: 8),
                          Text(
                            'Raised so far: ${formatKm(widget.partner!.currentAmount)}. This changes only when a donation is paid.',
                            style: const TextStyle(color: AppColors.textMuted),
                          ),
                        ],
                        SwitchListTile(
                          contentPadding: EdgeInsets.zero,
                          title: const Text('Active'),
                          subtitle: const Text('Inactive partners are hidden from the mobile donation list.'),
                          value: _isActive,
                          onChanged: (value) => setState(() => _isActive = value),
                        ),
                        if (_formError != null) Text(_formError!, style: const TextStyle(color: AppColors.danger)),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(onPressed: _saving ? null : () => Navigator.pop(context), child: const Text('Cancel')),
                    const SizedBox(width: 8),
                    FilledButton(onPressed: _saving ? null : _save, child: Text(_saving ? 'Saving...' : 'Save')),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

String _text(String? value) {
  final text = value?.trim() ?? '';
  return text.isEmpty ? '—' : text;
}
