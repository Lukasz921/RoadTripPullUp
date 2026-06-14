import { useState } from 'react';
import TextInput from '../../../components/ui/TextInput';
import ProfileRow from './ProfileRow';
import { updateCurrentUser, type CurrentUser, type Sex, type UpdateUserDTO } from '../../../api/user';

function calcAge(dateOfBirth: string): number {
  const birth = new Date(dateOfBirth);
  const today = new Date();
  let age = today.getFullYear() - birth.getFullYear();
  const monthDiff = today.getMonth() - birth.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) age--;
  return Math.max(0, age);
}

interface EditState {
  name: string;
  surname: string;
  phoneNumber: string;
  dateOfBirth: string;
  sex: Sex | '';
}

function toEditState(user: CurrentUser): EditState {
  return {
    name: user.name,
    surname: user.surname,
    phoneNumber: user.phoneNumber ?? '',
    dateOfBirth: user.dateOfBirth.slice(0, 10),
    sex: (user.sex as Sex) || '',
  };
}

interface ProfileDetailsProps {
  user: CurrentUser;
  onUpdated: () => void;
}

export default function ProfileDetails({ user, onUpdated }: ProfileDetailsProps) {
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState<EditState>(() => toEditState(user));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  function updateField(field: keyof EditState, value: string) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function startEditing() {
    setForm(toEditState(user));
    setError('');
    setEditing(true);
  }

  function cancelEditing() {
    setEditing(false);
    setError('');
  }

  async function save() {
    setSaving(true);
    setError('');

    const dto: UpdateUserDTO = {
      name: form.name,
      surname: form.surname,
      phoneNumber: form.phoneNumber || undefined,
      dateOfBirth: form.dateOfBirth || undefined,
      sex: form.sex || undefined,
    };

    try {
      await updateCurrentUser(dto);
      onUpdated();
      setEditing(false);
    } catch (err) {
      console.error('Failed to update profile:', err);
      setError('Failed to save changes. Please try again.');
    } finally {
      setSaving(false);
    }
  }

  if (editing) {
    return (
      <div>
        {error && <p className="mb-4 rounded-lg bg-red-50 px-4 py-2 text-sm text-red-600">{error}</p>}

        <div className="grid gap-4 sm:grid-cols-2">
          <TextInput label="Name" value={form.name} onChange={(v) => updateField('name', v)} />
          <TextInput label="Surname" value={form.surname} onChange={(v) => updateField('surname', v)} />
          <TextInput
            label="Phone"
            type="tel"
            value={form.phoneNumber}
            onChange={(v) => updateField('phoneNumber', v)}
            placeholder="+48 123 456 789"
          />
          <TextInput
            label="Date of birth"
            type="date"
            value={form.dateOfBirth}
            onChange={(v) => updateField('dateOfBirth', v)}
          />
          <label className="block">
            <span className="text-sm text-[#5d7056]">Sex</span>
            <select
              value={form.sex}
              onChange={(e) => updateField('sex', e.target.value)}
              className="mt-1 h-12 w-full rounded-xl border border-[#d7e8c8] bg-white px-4 font-semibold text-[#12351f] outline-none focus:border-[#8cc63f]"
            >
              <option value="" disabled>Select…</option>
              <option value="MALE">Male</option>
              <option value="FEMALE">Female</option>
              <option value="OTHER">Other</option>
            </select>
          </label>
        </div>

        <div className="mt-6 flex gap-2">
          <button
            onClick={save}
            disabled={saving}
            className="rounded-xl bg-[#12351f] px-4 py-2 text-sm font-semibold text-white hover:bg-[#1d4a2d] disabled:opacity-60"
          >
            {saving ? 'Saving…' : 'Save changes'}
          </button>
          <button
            onClick={cancelEditing}
            disabled={saving}
            className="rounded-xl bg-white px-4 py-2 text-sm font-semibold text-[#12351f] shadow-sm ring-1 ring-[#d7e8c8] hover:bg-[#f3faee] disabled:opacity-60"
          >
            Cancel
          </button>
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className="grid gap-4 sm:grid-cols-2">
        <ProfileRow label="Name" value={user.name} />
        <ProfileRow label="Surname" value={user.surname} />
        <ProfileRow label="Age" value={calcAge(user.dateOfBirth)} />
        <ProfileRow label="Sex" value={user.sex} />
        <ProfileRow label="Email" value={user.email} />
        {user.phoneNumber && <ProfileRow label="Phone" value={user.phoneNumber} />}
        <ProfileRow label="Rating" value={user.avgRating > 0 ? user.avgRating.toFixed(2) : 'No rating'} />
        <ProfileRow label="Ratings count" value={user.ratingsCount} />
      </div>

      <div className="mt-6">
        <button
          onClick={startEditing}
          className="rounded-xl bg-white px-4 py-2 text-sm font-semibold text-[#12351f] shadow-sm ring-1 ring-[#d7e8c8] hover:bg-[#f3faee]"
        >
          Edit profile
        </button>
      </div>
    </div>
  );
}
