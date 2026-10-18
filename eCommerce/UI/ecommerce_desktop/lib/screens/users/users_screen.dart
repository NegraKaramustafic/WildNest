import 'dart:async';

import 'package:ecommerce_desktop/models/app_user.dart';
import 'package:ecommerce_desktop/providers/auth_provider.dart';
import 'package:ecommerce_desktop/providers/user_provider.dart';
import 'package:ecommerce_desktop/screens/users/user_form_dialog.dart';
import 'package:ecommerce_desktop/theme/app_colors.dart';
import 'package:ecommerce_desktop/utils/api_client_exception.dart';
import 'package:ecommerce_desktop/utils/formatters.dart';
import 'package:ecommerce_desktop/widgets/admin_list.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

class UsersScreen extends StatefulWidget {
  const UsersScreen({super.key});

  @override
  State<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  static const _pageSize = 10;

  final _searchController = TextEditingController();
  Timer? _debounce;
  int _requestId = 0;
  int _page = 1;
  int _total = 0;
  String? _role;
  String? _status;
  bool _loading = true;
  String? _error;
  List<AppUser> _users = [];

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
      final result = await context.read<UserProvider>().get(
        filter: {
          'FTS': _searchController.text.trim(),
          'Role': _role,
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
        _users = items;
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

  Future<void> _edit({AppUser? user}) async {
    final selfId = context.read<AuthProvider>().userId;
    final saved = await showDialog<AppUser>(
      context: context,
      builder: (_) => UserFormDialog(user: user, editingSelf: user != null && user.id == selfId),
    );
    if (saved == null || !mounted) return;
    _toast(user == null ? 'User added.' : 'User updated.');
    if (user == null) _page = 1;
    await _load();
  }

  Future<void> _setActive(AppUser user, {required bool active}) async {
    final confirmed = await confirmAdminAction(
      context: context,
      title: active ? 'Unblock user' : 'Block user',
      message: active
          ? 'Unblock ${user.fullName}? They will be able to sign in again.'
          : 'Block ${user.fullName}? They will not be able to sign in, and their current sessions will stop working.',
      confirmLabel: active ? 'Unblock' : 'Block',
      destructive: !active,
    );
    if (!confirmed || !mounted) return;
    try {
      await context.read<UserProvider>().setActive(user.id, active);
      if (!mounted) return;
      _toast(active ? 'User unblocked.' : 'User blocked.');
      await _load();
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      _toast(friendlyError(error));
    }
  }

  Future<void> _delete(AppUser user) async {
    final confirmed = await confirmAdminAction(
      context: context,
      title: 'Delete user',
      message:
          'Delete ${user.fullName}? Accounts with related records are blocked instead of deleted.',
      confirmLabel: 'Delete',
      destructive: true,
    );
    if (!confirmed || !mounted) return;
    try {
      await context.read<UserProvider>().remove(user.id);
      if (!mounted) return;
      _toast('User removed. Accounts with related records are blocked instead of deleted.');
      await _load();
    } on SessionExpiredException {
      return;
    } catch (error) {
      if (!mounted) return;
      _toast(friendlyError(error));
    }
  }

  Future<void> _details(AppUser user) async {
    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(user.fullName),
        content: SizedBox(
          width: 480,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Align(
                alignment: Alignment.centerLeft,
                child: EntityThumbnail(base64Image: user.profileImageBase64),
              ),
              const SizedBox(height: 12),
              DetailLine(label: 'Username', value: _text(user.username)),
              DetailLine(label: 'Email', value: _text(user.email)),
              DetailLine(label: 'Phone', value: _text(user.phoneNumber)),
              DetailLine(label: 'Role', value: _text(user.role)),
              DetailLine(label: 'Status', value: user.isActive ? 'Active' : 'Blocked'),
              DetailLine(label: 'Created', value: formatDateTime(user.createdAt)),
              DetailLine(label: 'Last sign-in', value: formatDateTime(user.lastLoginAt)),
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
      title: 'Users',
      subtitle: 'Create accounts, edit details, and block sign-in.',
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
                hintText: 'Search by name, username, or email...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
            ),
          ),
          SizedBox(width: 180, child: _roleFilter()),
          SizedBox(width: 180, child: _statusFilter()),
          FilledButton.icon(
            onPressed: () => _edit(),
            icon: const Icon(Icons.add, size: 18),
            label: const Text('Add user'),
          ),
        ],
      ),
      body: _body(),
    );
  }

  Widget _body() {
    if (_loading) return const Center(child: CircularProgressIndicator());
    if (_error != null) return ListError(message: _error!, onRetry: _load);
    final selfId = context.watch<AuthProvider>().userId;
    const headerStyle = TextStyle(fontSize: 12, fontWeight: FontWeight.w700, color: AppColors.textMuted);
    return AdminTableCard(
      minWidth: 1180,
      empty: 'No users to show.',
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
            Expanded(flex: 2, child: Text('Username', style: headerStyle)),
            Expanded(flex: 3, child: Text('Email', style: headerStyle)),
            Expanded(flex: 2, child: Text('Role', style: headerStyle)),
            Expanded(flex: 2, child: Text('Status', style: headerStyle)),
            SizedBox(width: 168, child: Text('Actions', style: headerStyle)),
          ],
        ),
      ),
      rows: [
        for (final user in _users)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                EntityThumbnail(base64Image: user.profileImageBase64),
                const SizedBox(width: 12),
                Expanded(flex: 3, child: EllipsisText(user.fullName, style: const TextStyle(fontWeight: FontWeight.w600))),
                Expanded(flex: 2, child: EllipsisText(_text(user.username))),
                Expanded(flex: 3, child: EllipsisText(_text(user.email))),
                Expanded(flex: 2, child: EllipsisText(_text(user.role))),
                Expanded(
                  flex: 2,
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: labeledStatusChip(
                      label: user.isActive ? 'Active' : 'Blocked',
                      tone: user.isActive ? 'success' : 'danger',
                    ),
                  ),
                ),
                SizedBox(
                  width: 168,
                  child: Row(
                    children: [
                      IconActionButton(
                        icon: Icons.visibility_outlined,
                        color: AppColors.text,
                        tooltip: 'View details',
                        enabled: true,
                        onPressed: () => _details(user),
                      ),
                      IconActionButton(
                        icon: Icons.edit_outlined,
                        color: AppColors.text,
                        tooltip: 'Edit',
                        enabled: true,
                        onPressed: () => _edit(user: user),
                      ),
                      IconActionButton(
                        icon: user.isActive ? Icons.block : Icons.lock_open,
                        color: AppColors.danger,
                        tooltip: user.id == selfId
                            ? 'You cannot block your own account.'
                            : (user.isActive ? 'Block' : 'Unblock'),
                        enabled: user.id != selfId,
                        onPressed: () => _setActive(user, active: !user.isActive),
                      ),
                      IconActionButton(
                        icon: Icons.delete_outline,
                        color: AppColors.danger,
                        tooltip: user.id == selfId ? 'You cannot delete your own account.' : 'Delete',
                        enabled: user.id != selfId,
                        onPressed: () => _delete(user),
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

  Widget _roleFilter() {
    return DropdownButtonFormField<String?>(
      key: ValueKey('user-role-$_role'),
      initialValue: _role,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Role', isDense: true),
      items: const [
        DropdownMenuItem(value: null, child: Text('All roles', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'Admin', child: Text('Admin', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'Customer', child: Text('Customer', overflow: TextOverflow.ellipsis)),
      ],
      onChanged: (value) => _apply(() => _role = value),
    );
  }

  Widget _statusFilter() {
    return DropdownButtonFormField<String?>(
      key: ValueKey('user-status-$_status'),
      initialValue: _status,
      isExpanded: true,
      decoration: const InputDecoration(labelText: 'Status', isDense: true),
      items: const [
        DropdownMenuItem(value: null, child: Text('All statuses', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'active', child: Text('Active', overflow: TextOverflow.ellipsis)),
        DropdownMenuItem(value: 'blocked', child: Text('Blocked', overflow: TextOverflow.ellipsis)),
      ],
      onChanged: (value) => _apply(() => _status = value),
    );
  }
}

String _text(String? value) {
  final text = value?.trim() ?? '';
  return text.isEmpty ? '—' : text;
}
