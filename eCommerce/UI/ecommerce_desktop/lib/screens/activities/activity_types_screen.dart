import 'dart:async';

import 'package:ecommerce_desktop/models/activity_type.dart';
import 'package:ecommerce_desktop/providers/activity_type_provider.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/json_parsers.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class ActivityTypesScreen extends StatefulWidget {
  const ActivityTypesScreen({super.key});

  @override
  State<ActivityTypesScreen> createState() => _ActivityTypesScreenState();
}

class _ActivityTypesScreenState extends State<ActivityTypesScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  bool _loading = true;
  String? _error;
  List<ActivityType> _types = [];

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
      final result = await context.read<ActivityTypeProvider>().get(
        filter: {
          'Name': _searchController.text.trim(),
          'Page': _page,
          'PageSize': _pageSize,
          'IncludeTotalCount': true,
        },
      );
      if (!mounted || requestId != _requestId) return;
      final items = result.items ?? [];
      final total = result.totalCount ?? items.length;
      if (items.isEmpty && total > 0 && _page > 1) {
        setState(() => _page = _page - 1);
        await _load();
        return;
      }
      setState(() {
        _types = items;
        _total = total;
        _loading = false;
      });
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = _message(e);
        _loading = false;
      });
    }
  }

  void _onSearchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () {
      if (!mounted) return;
      setState(() => _page = 1);
      _load();
    });
  }

  void _changePage(int page) {
    setState(() => _page = page);
    _load();
  }

  Future<void> _edit({ActivityType? type}) async {
    final saved = await showDialog<ActivityType>(
      context: context,
      builder: (_) => _ActivityTypeDialog(type: type),
    );
    if (saved == null || !mounted) return;

    final created = type == null;
    _toast(created ? 'Tip aktivnosti je dodan.' : 'Tip aktivnosti je ažuriran.');
    if (created) {
      final query = _searchController.text.trim().toLowerCase();
      if (query.isNotEmpty && !saved.name.toLowerCase().contains(query)) {
        _searchController.clear();
      }
      _page = 1;
    }
    await _load();
    if (!created || !mounted) return;
    if (_types.every((item) => item.id != saved.id)) {
      _searchController.text = saved.name;
      _page = 1;
      await _load();
    }
  }

  Future<void> _delete(ActivityType type) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Brisanje tipa aktivnosti'),
        content: Text('Obrisati tip aktivnosti ${type.name}?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Odustani'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Obriši'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      await context.read<ActivityTypeProvider>().remove(type.id);
      if (!mounted) return;
      _toast('Tip aktivnosti je obrisan.');
      await _load();
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      _toast(_message(e));
    }
  }

  void _toast(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  String _message(Object error) {
    return error.toString().replaceFirst('Bad state: ', '');
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Tipovi aktivnosti',
                      style: TextStyle(fontSize: 26, fontWeight: FontWeight.w700),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'Šifarnik tipova za obrazac aktivnosti',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  ],
                ),
              ),
              FilledButton.icon(
                onPressed: () => _edit(),
                icon: const Icon(Icons.add, size: 18),
                label: const Text('Dodaj tip'),
              ),
            ],
          ),
          const SizedBox(height: 18),
          SizedBox(
            width: 360,
            child: TextField(
              controller: _searchController,
              onChanged: _onSearchChanged,
              decoration: const InputDecoration(
                hintText: 'Pretraži tipove aktivnosti...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          const SizedBox(height: 16),
          Expanded(child: _body()),
        ],
      ),
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(_error!, textAlign: TextAlign.center),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _load, child: const Text('Pokušaj ponovo')),
          ],
        ),
      );
    }

    final from = _total == 0 ? 0 : ((_page - 1) * _pageSize) + 1;
    final to = (_page * _pageSize) > _total ? _total : _page * _pageSize;
    final hasPrev = _page > 1;
    final hasNext = _page * _pageSize < _total;

    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        children: [
          const Padding(
            padding: EdgeInsets.symmetric(horizontal: 18, vertical: 14),
            child: Row(
              children: [
                Expanded(flex: 3, child: _HeaderLabel('Naziv')),
                Expanded(flex: 4, child: _HeaderLabel('Opis')),
                SizedBox(width: 96, child: _HeaderLabel('Akcije')),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: _types.isEmpty
                ? const Center(
                    child: Text(
                      'Nema tipova aktivnosti.',
                      style: TextStyle(color: AppColors.textMuted),
                    ),
                  )
                : ListView.separated(
                    itemCount: _types.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (context, index) {
                      final type = _types[index];
                      return Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 10),
                        child: Row(
                          children: [
                            Expanded(flex: 3, child: Text(type.name)),
                            Expanded(
                              flex: 4,
                              child: Text(
                                type.description?.trim().isNotEmpty == true
                                    ? type.description!
                                    : '—',
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(color: AppColors.textMuted),
                              ),
                            ),
                            SizedBox(
                              width: 96,
                              child: Row(
                                children: [
                                  IconButton(
                                    tooltip: 'Uredi',
                                    onPressed: () => _edit(type: type),
                                    icon: const Icon(Icons.edit_outlined, size: 18),
                                  ),
                                  _DeleteAction(
                                    enabled: !type.isInUse,
                                    tooltip: type.isInUse
                                        ? 'Tip aktivnosti koriste aktivnosti i ne može se obrisati.'
                                        : 'Obriši',
                                    onPressed: () => _delete(type),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      );
                    },
                  ),
          ),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            child: Row(
              children: [
                Text(
                  _total == 0 ? '0 od 0' : '$from–$to od $_total',
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 13),
                ),
                const Spacer(),
                IconButton(
                  tooltip: 'Prethodna',
                  onPressed: hasPrev ? () => _changePage(_page - 1) : null,
                  icon: const Icon(Icons.chevron_left),
                ),
                Text('$_page'),
                IconButton(
                  tooltip: 'Sljedeća',
                  onPressed: hasNext ? () => _changePage(_page + 1) : null,
                  icon: const Icon(Icons.chevron_right),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _HeaderLabel extends StatelessWidget {
  const _HeaderLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(
      text.toUpperCase(),
      style: const TextStyle(
        fontSize: 11,
        letterSpacing: 0.6,
        fontWeight: FontWeight.w600,
        color: AppColors.textMuted,
      ),
    );
  }
}

class _DeleteAction extends StatelessWidget {
  const _DeleteAction({
    required this.enabled,
    required this.tooltip,
    required this.onPressed,
  });

  final bool enabled;
  final String tooltip;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: InkWell(
        onTap: enabled ? onPressed : () {},
        borderRadius: BorderRadius.circular(8),
        child: SizedBox(
          width: 40,
          height: 40,
          child: Icon(
            Icons.delete_outline,
            size: 18,
            color: enabled ? AppColors.danger : AppColors.danger.withValues(alpha: 0.35),
          ),
        ),
      ),
    );
  }
}

class _ActivityTypeDialog extends StatefulWidget {
  const _ActivityTypeDialog({this.type});

  final ActivityType? type;

  @override
  State<_ActivityTypeDialog> createState() => _ActivityTypeDialogState();
}

class _ActivityTypeDialogState extends State<_ActivityTypeDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _name;
  late final TextEditingController _description;
  bool _saving = false;
  String? _nameError;

  bool get _isEdit => widget.type != null;

  @override
  void initState() {
    super.initState();
    _name = TextEditingController(text: widget.type?.name ?? '');
    _description = TextEditingController(text: widget.type?.description ?? '');
  }

  @override
  void dispose() {
    _name.dispose();
    _description.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _nameError = null);
    if (!_formKey.currentState!.validate()) return;

    final body = {
      'name': _name.text.trim(),
      'description': blankToNull(_description.text),
    };

    setState(() => _saving = true);
    try {
      final provider = context.read<ActivityTypeProvider>();
      final ActivityType saved;
      if (_isEdit) {
        saved = await provider.update(widget.type!.id, body);
      } else {
        saved = await provider.insert(body);
      }
      if (mounted) Navigator.pop(context, saved);
    } on SessionExpiredException {
      return;
    } catch (e) {
      if (!mounted) return;
      final message = e.toString().replaceFirst('Bad state: ', '');
      setState(() {
        _saving = false;
        _nameError = message;
      });
      _formKey.currentState!.validate();
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(_isEdit ? 'Uredi tip aktivnosti' : 'Novi tip aktivnosti'),
      content: SizedBox(
        width: 420,
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextFormField(
                controller: _name,
                autofocus: true,
                decoration: const InputDecoration(labelText: 'Naziv'),
                onChanged: (_) {
                  if (_nameError != null) setState(() => _nameError = null);
                },
                validator: (value) {
                  if (_nameError != null) return _nameError;
                  final text = value?.trim() ?? '';
                  if (text.isEmpty) return 'Naziv je obavezan.';
                  if (text.length < 2) return 'Naziv mora imati najmanje 2 znaka.';
                  if (text.length > 50) return 'Naziv ne može biti duži od 50 znakova.';
                  return null;
                },
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _description,
                maxLines: 3,
                decoration: const InputDecoration(
                  labelText: 'Opis',
                  alignLabelWithHint: true,
                ),
                validator: (value) {
                  if ((value?.trim().length ?? 0) > 500) {
                    return 'Opis ne može biti duži od 500 znakova.';
                  }
                  return null;
                },
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _saving ? null : () => Navigator.pop(context),
          child: const Text('Odustani'),
        ),
        FilledButton(
          onPressed: _saving ? null : _save,
          child: Text(_saving ? 'Spremanje...' : 'Sačuvaj'),
        ),
      ],
    );
  }
}
