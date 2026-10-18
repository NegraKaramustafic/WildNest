import 'dart:async';
import 'dart:typed_data';

import 'package:ecommerce_desktop/models/badge.dart';
import 'package:ecommerce_desktop/models/user_badge.dart';
import 'package:ecommerce_desktop/providers/badge_provider.dart';
import 'package:ecommerce_desktop/providers/user_badge_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/form_rules.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/utils/image_bytes.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:ecommerce_desktop/widgets/image_field.dart';
import 'package:flutter/material.dart' hide Badge;
import 'package:provider/provider.dart';

class BadgesScreen extends StatefulWidget {
  const BadgesScreen({super.key});

  @override
  State<BadgesScreen> createState() => _BadgesScreenState();
}

class _BadgesScreenState extends State<BadgesScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  String? _criteria;
  bool _loading = true;
  String? _error;
  List<Badge> _badges = [];

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
      final result = await context.read<BadgeProvider>().get(
        filter: {
          'Name': _searchController.text.trim(),
          'Criteria': _criteria,
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
        _badges = items;
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

  Future<void> _edit({Badge? badge}) async {
    final saved = await showDialog<Badge>(
      context: context,
      builder: (_) => _BadgeDialog(badge: badge),
    );
    if (saved == null || !mounted) return;
    _toast(badge == null ? 'Badge added.' : 'Badge updated.');
    if (badge == null) _page = 1;
    await _load();
  }

  Future<void> _delete(Badge badge) async {
    final confirmed = await confirmAdminAction(
      context: context,
      title: 'Delete badge',
      message: 'Delete ${badge.name}?',
      confirmLabel: 'Delete',
      destructive: true,
    );
    if (!confirmed || !mounted) return;
    try {
      await context.read<BadgeProvider>().remove(badge.id);
      if (!mounted) return;
      _toast('Badge deleted.');
      await _load();
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      _toast(friendlyError(error));
    }
  }

  Future<void> _holders(Badge badge) async {
    await showDialog<void>(
      context: context,
      builder: (_) => _HoldersDialog(badge: badge),
    );
  }

  void _toast(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    return AdminScreenFrame(
      title: 'Badges',
      subtitle: 'Award rules used after a completed adoption, booking, or donation.',
      filters: Wrap(
        spacing: 12,
        runSpacing: 12,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          SizedBox(
            width: 280,
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
                hintText: 'Search badges...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          SizedBox(width: 220, child: _criteriaFilter()),
          FilledButton.icon(
            onPressed: () => _edit(),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Add badge'),
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
      empty: 'No badges to show.',
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
            Expanded(flex: 4, child: Text('Awarded when', style: headerStyle)),
            Expanded(flex: 2, child: Text('Holders', style: headerStyle)),
            SizedBox(width: 168, child: Text('Actions', style: headerStyle)),
          ],
        ),
      ),
      rows: [
        for (final badge in _badges)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                EntityThumbnail(url: badge.iconUrl),
                const SizedBox(width: 12),
                Expanded(
                  flex: 3,
                  child: EllipsisText(badge.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                ),
                Expanded(flex: 4, child: EllipsisText(badgeAwardRule(badge.criteria))),
                Expanded(flex: 2, child: EllipsisText('${badge.holderCount}')),
                SizedBox(
                  width: 168,
                  child: Row(
                    children: [
                      IconActionButton(
                        icon: Icons.people_outline,
                        color: AppColors.text,
                        tooltip: 'View holders',
                        enabled: true,
                        onPressed: () => _holders(badge),
                      ),
                      IconActionButton(
                        icon: Icons.edit_outlined,
                        color: AppColors.text,
                        tooltip: 'Edit',
                        enabled: true,
                        onPressed: () => _edit(badge: badge),
                      ),
                      IconActionButton(
                        icon: Icons.delete_outline,
                        color: AppColors.danger,
                        tooltip: badge.isInUse
                            ? 'This badge has been awarded to users and cannot be deleted.'
                            : 'Delete',
                        enabled: !badge.isInUse,
                        onPressed: () => _delete(badge),
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

  Widget _criteriaFilter() {
    return DropdownButtonFormField<String?>(
      key: ValueKey('badge-criteria-$_criteria'),
      initialValue: _criteria,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Award rule', isDense: true),
      items: [
        const DropdownMenuItem(value: null, child: Text('All rules', overflow: TextOverflow.ellipsis)),
        for (final criteria in badgeCriteriaValues)
          DropdownMenuItem(
            value: criteria,
            child: Text(badgeCriteriaLabel(criteria), overflow: TextOverflow.ellipsis),
          ),
      ],
      onChanged: (value) => _apply(() => _criteria = value),
    );
  }
}

class _BadgeDialog extends StatefulWidget {
  const _BadgeDialog({this.badge});

  final Badge? badge;

  @override
  State<_BadgeDialog> createState() => _BadgeDialogState();
}

class _BadgeDialogState extends State<_BadgeDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _description;
  late String _criteria;
  Uint8List? _pickedBytes;
  String? _pickedBase64;
  String? _pickedType;
  String? _existingUrl;
  bool _clearImage = false;
  bool _saving = false;
  String? _formError;
  String? _imageError;

  bool get _isEdit => widget.badge != null;

  @override
  void initState() {
    super.initState();
    _name = TextEditingController(text: widget.badge?.name ?? '');
    _description = TextEditingController(text: widget.badge?.description ?? '');
    _criteria = badgeCriteriaValues.contains(widget.badge?.criteria)
        ? widget.badge!.criteria
        : badgeCriteriaValues.first;
    _existingUrl = widget.badge?.iconUrl;
  }

  @override
  void dispose() {
    _name.dispose();
    _description.dispose();
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
      'description': _description.text.trim().isEmpty ? null : _description.text.trim(),
      'criteria': _criteria,
      'iconUrl': _clearImage ? '' : _existingUrl,
      'imageBase64': _pickedBase64,
      'imageContentType': _pickedType,
    };
    try {
      final provider = context.read<BadgeProvider>();
      final saved = _isEdit
          ? await provider.update(widget.badge!.id, body)
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
        constraints: const BoxConstraints(maxWidth: 560, maxHeight: 720),
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
                        _isEdit ? 'Edit badge' : 'New badge',
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
                          controller: _description,
                          maxLines: 3,
                          maxLength: 300,
                          autovalidateMode: AutovalidateMode.onUserInteraction,
                          decoration: const InputDecoration(
                            labelText: 'Description (optional)',
                            alignLabelWithHint: true,
                            errorMaxLines: 2,
                          ),
                          validator: (value) => optionalText(value, 'Description', 300),
                        ),
                        const SizedBox(height: 12),
                        DropdownButtonFormField<String>(
                          key: ValueKey('criteria-$_criteria'),
                          initialValue: _criteria,
                          isExpanded: true,
                          decoration: const InputDecoration(labelText: 'Award rule', isDense: true),
                          items: [
                            for (final criteria in badgeCriteriaValues)
                              DropdownMenuItem(
                                value: criteria,
                                child: Text(badgeCriteriaLabel(criteria), overflow: TextOverflow.ellipsis),
                              ),
                          ],
                          onChanged: (value) {
                            if (value != null) setState(() => _criteria = value);
                          },
                        ),
                        const SizedBox(height: 8),
                        Text(badgeAwardRule(_criteria), style: const TextStyle(color: AppColors.textMuted)),
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

class _HoldersDialog extends StatefulWidget {
  const _HoldersDialog({required this.badge});

  final Badge badge;

  @override
  State<_HoldersDialog> createState() => _HoldersDialogState();
}

class _HoldersDialogState extends State<_HoldersDialog> {
  static const _pageSize = 8;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _page = 1;
  int _total = 0;
  bool _loading = true;
  String? _error;
  List<UserBadge> _holders = [];

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
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await context.read<UserBadgeProvider>().get(
        filter: {
          'BadgeId': widget.badge.id,
          'FTS': _searchController.text.trim(),
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted) return;
      setState(() {
        _holders = result.items ?? [];
        _total = result.totalCount ?? _holders.length;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _error = friendlyError(error);
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text('${widget.badge.name} holders'),
      content: SizedBox(
        width: 480,
        height: 420,
        child: Column(
          children: [
            Text(badgeAwardRule(widget.badge.criteria), style: const TextStyle(color: AppColors.textMuted)),
            const SizedBox(height: 12),
            TextField(
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
                hintText: 'Search holders...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
            const SizedBox(height: 12),
            Expanded(child: _body()),
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Close')),
      ],
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) return ListError(message: _error!, onRetry: _load);
    if (_holders.isEmpty) {
      return const Center(child: Text('No users have this badge.', style: TextStyle(color: AppColors.textMuted)));
    }
    return Column(
      children: [
        Expanded(
          child: ListView.separated(
            itemCount: _holders.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (context, index) {
              final holder = _holders[index];
              final name = holder.userFullName.trim().isEmpty ? '—' : holder.userFullName;
              return ListTile(
                contentPadding: EdgeInsets.zero,
                title: EllipsisText(name),
                subtitle: EllipsisText('Earned ${formatDateTime(holder.earnedAt)}'),
              );
            },
          ),
        ),
        ListPager(
          page: _page,
          pageSize: _pageSize,
          total: _total,
          onPage: (page) {
            setState(() => _page = page);
            _load();
          },
        ),
      ],
    );
  }
}
