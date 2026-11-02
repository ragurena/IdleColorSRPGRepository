using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

// 倒れたコピーの霊体。本体の所持やバトルはそのまま。回復すると所持が1増える。
// 同じキャラIDの霊体は、倒れるたびに別々に入る。
public sealed class HospitalPatient
{
    public ulong Id;
    public uint CharacterId;
    public int OpaquePixels;
    public int Size;
    public double HealedPixels;
    public long AdmittedUnixMs;
    public long BeddedUnixMs;
    public long LastCalcUnixMs;
    public int Bed;
}

public struct HospitalStep
{
    public int Tapped;
    public int Returned;
}

public sealed class HospitalState
{
    public const int InitialBeds = 2;
    public const int MaxBeds = 8;
    public const int MaxCards = (MaxBeds + 1) / 2;

    public int UnlockedBeds = InitialBeds;

    readonly List<HospitalPatient> _patients = new List<HospitalPatient>();
    readonly List<uint> _pending = new List<uint>();
    readonly List<string> _grants = new List<string>();
    readonly uint[] _nurses = new uint[MaxCards + 1];
    readonly uint[] _nursePersons = new uint[MaxCards + 1];
    readonly int[] _nurseSizes = new int[MaxCards + 1];
    readonly List<NurseGrowth> _growth = new List<NurseGrowth>();
    ulong _nextPatientId = 1;

    sealed class NurseGrowth
    {
        public bool Person;
        public uint Id;
        public int Experience;
    }

    public int WaitingCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _patients.Count; i++)
            {
                if (_patients[i].Bed == 0)
                    count++;
            }
            return count;
        }
    }

    public int PendingCount
    {
        get { return _pending.Count; }
    }

    public int PatientCount
    {
        get { return _patients.Count; }
    }

    public static long NowUnixMs()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    public void Reset()
    {
        UnlockedBeds = InitialBeds;
        _patients.Clear();
        _pending.Clear();
        _grants.Clear();
        for (int i = 0; i < _nurses.Length; i++)
        {
            _nurses[i] = 0;
            _nursePersons[i] = 0;
            _nurseSizes[i] = 0;
        }
        _growth.Clear();
        _nextPatientId = 1;
    }

    public uint NurseOnBed(int bed)
    {
        int card = CardOf(bed);
        if (card < 1 || card > MaxCards)
            return 0;
        return _nurses[card];
    }

    public uint PersonOnBed(int bed)
    {
        int card = CardOf(bed);
        if (card < 1 || card > MaxCards)
            return 0;
        return _nursePersons[card];
    }

    public int NurseSize(int bed)
    {
        int card = CardOf(bed);
        if (card < 1 || card > MaxCards)
            return 0;
        return _nurseSizes[card];
    }

    public int NurseCard(uint characterId)
    {
        if (characterId == 0)
            return 0;
        for (int card = 1; card <= MaxCards; card++)
        {
            if (_nurses[card] == characterId)
                return card;
        }
        return 0;
    }

    public int PersonCard(uint personId)
    {
        if (personId == 0)
            return 0;
        for (int card = 1; card <= MaxCards; card++)
        {
            if (_nursePersons[card] == personId)
                return card;
        }
        return 0;
    }

    public int Experience(bool person, uint id)
    {
        int index = FindGrowth(person, id);
        if (index < 0)
            return 0;
        return _growth[index].Experience;
    }

    public void AddExperience(bool person, uint id, int amount)
    {
        if (id == 0 || amount == 0)
            return;
        int index = FindGrowth(person, id);
        if (index < 0)
        {
            if (amount < 0)
                return;
            NurseGrowth growth = new NurseGrowth();
            growth.Person = person;
            growth.Id = id;
            growth.Experience = amount;
            _growth.Add(growth);
            return;
        }
        long sum = (long)_growth[index].Experience + amount;
        if (sum < 0)
            sum = 0;
        if (sum > int.MaxValue)
            sum = int.MaxValue;
        _growth[index].Experience = (int)sum;
    }

    public static int CardOf(int bed)
    {
        if (bed < 1)
            return 0;
        return (bed + 1) / 2;
    }

    public bool IsNurse(uint characterId)
    {
        return NurseCard(characterId) != 0;
    }

    public bool IsPersonNurse(uint personId)
    {
        return PersonCard(personId) != 0;
    }

    public HospitalPatient PatientOnBed(int bed)
    {
        if (bed < 1)
            return null;
        for (int i = 0; i < _patients.Count; i++)
        {
            if (_patients[i].Bed == bed)
                return _patients[i];
        }
        return null;
    }

    public void CopyWaiting(List<HospitalPatient> into)
    {
        if (into == null)
            return;
        into.Clear();
        for (int i = 0; i < _patients.Count; i++)
        {
            if (_patients[i].Bed == 0)
                into.Add(_patients[i]);
        }
        into.Sort(CompareQueue);
    }

    public int MillisecondsPerPixel(HospitalPatient patient)
    {
        if (patient == null || patient.Bed < 1)
            return HospitalMath.MillisecondsWithoutNurse;
        int card = CardOf(patient.Bed);
        if (card < 1 || card > MaxCards)
            return HospitalMath.MillisecondsWithoutNurse;
        bool person = _nurses[card] == 0 && _nursePersons[card] != 0;
        uint id = person ? _nursePersons[card] : _nurses[card];
        if (id == 0)
            return HospitalMath.MillisecondsWithoutNurse;
        if (person)
            return HospitalMath.MillisecondsForPerson(Experience(true, id));
        int baseSize = _nurseSizes[card];
        if (baseSize < 1)
            baseSize = 1;
        return HospitalMath.MillisecondsForSize(baseSize);
    }

    public double RemainingSeconds(HospitalPatient patient)
    {
        if (patient == null)
            return 0;
        return HospitalMath.RemainingSeconds(patient.HealedPixels, patient.OpaquePixels, MillisecondsPerPixel(patient));
    }

    public ulong Admit(uint characterId, int opaquePixels, int size, long nowMs, Func<uint, bool> tryReturn)
    {
        if (characterId == 0)
            return 0;
        if (opaquePixels < 0)
            opaquePixels = 0;
        if (size < 1)
            size = 1;
        if (nowMs < 0)
            nowMs = 0;

        HospitalPatient patient = new HospitalPatient();
        patient.Id = _nextPatientId++;
        patient.CharacterId = characterId;
        patient.OpaquePixels = opaquePixels;
        patient.Size = size;
        patient.HealedPixels = 0;
        patient.AdmittedUnixMs = nowMs;
        patient.BeddedUnixMs = 0;
        patient.LastCalcUnixMs = nowMs;
        patient.Bed = 0;
        _patients.Add(patient);
        FillEmptyBeds(nowMs);
        CatchUp(nowMs, tryReturn);
        return patient.Id;
    }

    public int CatchUp(long nowMs, Func<uint, bool> tryReturn)
    {
        if (nowMs < 0)
            nowMs = 0;
        SanitizeTimes(nowMs);
        int returned = FlushPending(tryReturn);
        int guard = _patients.Count + 4;
        while (guard-- > 0)
        {
            HospitalPatient soonest = null;
            long soonestAt = long.MaxValue;
            for (int i = 0; i < _patients.Count; i++)
            {
                HospitalPatient patient = _patients[i];
                if (patient.Bed == 0)
                    continue;
                long finish = FinishUnixMs(patient);
                if (finish <= nowMs && finish < soonestAt)
                {
                    soonest = patient;
                    soonestAt = finish;
                }
            }
            if (soonest == null)
                break;
            AdvanceBedsTo(soonestAt);
            int bed = soonest.Bed;
            Complete(soonest);
            FillBed(bed, soonestAt);
        }
        AdvanceBedsTo(nowMs);
        guard = _patients.Count + 2;
        while (guard-- > 0)
        {
            HospitalPatient done = null;
            for (int i = 0; i < _patients.Count; i++)
            {
                if (_patients[i].Bed != 0 && IsComplete(_patients[i]))
                {
                    done = _patients[i];
                    break;
                }
            }
            if (done == null)
                break;
            int bed = done.Bed;
            Complete(done);
            FillBed(bed, nowMs);
        }
        returned += FlushPending(tryReturn);
        return returned;
    }

    public HospitalStep Care(int cardIndex, long nowMs, Func<uint, bool> tryReturn)
    {
        int firstBed = cardIndex * 2 + 1;
        return CareBeds(nowMs, tryReturn, firstBed, firstBed + 1);
    }

    public HospitalStep CareBed(int bed, long nowMs, Func<uint, bool> tryReturn)
    {
        return CareBeds(nowMs, tryReturn, bed);
    }

    HospitalStep CareBeds(long nowMs, Func<uint, bool> tryReturn, params int[] beds)
    {
        if (nowMs < 0)
            nowMs = 0;
        if (beds == null)
            beds = new int[0];
        ulong[] ids = new ulong[beds.Length];
        for (int i = 0; i < beds.Length; i++)
            ids[i] = PatientIdOnBed(beds[i]);
        HospitalStep step = new HospitalStep();
        step.Returned = CatchUp(nowMs, tryReturn);
        for (int i = 0; i < ids.Length; i++)
            step.Tapped += TapPatient(FindPatient(ids[i]), nowMs) ? 1 : 0;
        if (step.Tapped > 0)
            step.Returned += CatchUp(nowMs, tryReturn);
        return step;
    }

    public int AssignNurse(int card, uint characterId, int nurseSize, long nowMs, Func<uint, bool> tryReturn)
    {
        int unlockedCards = (UnlockedBeds + 1) / 2;
        if (card < 1 || card > unlockedCards)
            return -1;
        if (characterId != 0)
        {
            int other = NurseCard(characterId);
            if (other != 0 && other != card)
                return -1;
        }
        int returned = CatchUp(nowMs, tryReturn);
        if (characterId == 0)
        {
            ClearNurse(card);
            return returned;
        }
        if (nurseSize < 1)
            nurseSize = 1;
        _nursePersons[card] = 0;
        _nurses[card] = characterId;
        _nurseSizes[card] = nurseSize;
        return returned;
    }

    public int AssignPerson(int card, uint personId, long nowMs, Func<uint, bool> tryReturn)
    {
        int unlockedCards = (UnlockedBeds + 1) / 2;
        if (card < 1 || card > unlockedCards || personId == 0)
            return -1;
        int other = PersonCard(personId);
        if (other != 0 && other != card)
            return -1;
        int returned = CatchUp(nowMs, tryReturn);
        _nurses[card] = 0;
        _nursePersons[card] = personId;
        _nurseSizes[card] = HospitalMath.PersonNurseSize;
        return returned;
    }

    public void FillMissingNurseSizes(Func<uint, int> sizeOf)
    {
        if (sizeOf == null)
            return;
        for (int card = 1; card <= MaxCards; card++)
        {
            if (_nurses[card] == 0 || _nurseSizes[card] >= 1)
                continue;
            int size = sizeOf(_nurses[card]);
            if (size < 1)
                size = 1;
            _nurseSizes[card] = size;
        }
    }

    public bool TryGrantBed(string sourceId, long nowMs, Func<uint, bool> tryReturn)
    {
        if (string.IsNullOrEmpty(sourceId) || sourceId.IndexOf(',') >= 0 || sourceId.IndexOf('\n') >= 0 || sourceId.IndexOf('\r') >= 0)
            return false;
        if (UnlockedBeds >= MaxBeds)
            return false;
        for (int i = 0; i < _grants.Count; i++)
        {
            if (_grants[i] == sourceId)
                return false;
        }
        _grants.Add(sourceId);
        UnlockedBeds++;
        if (nowMs < 0)
            nowMs = 0;
        FillEmptyBeds(nowMs);
        CatchUp(nowMs, tryReturn);
        return true;
    }

    public void Repair()
    {
        if (UnlockedBeds < InitialBeds)
            UnlockedBeds = InitialBeds;
        if (UnlockedBeds > MaxBeds)
            UnlockedBeds = MaxBeds;
        bool[] used = new bool[MaxBeds + 1];
        for (int i = 0; i < _patients.Count; i++)
        {
            HospitalPatient patient = _patients[i];
            if (patient.OpaquePixels < 0)
                patient.OpaquePixels = 0;
            if (patient.Size < 1)
                patient.Size = 1;
            if (patient.HealedPixels < 0)
                patient.HealedPixels = 0;
            if (patient.OpaquePixels > 0 && patient.HealedPixels > patient.OpaquePixels)
                patient.HealedPixels = patient.OpaquePixels;
            if (patient.AdmittedUnixMs < 0)
                patient.AdmittedUnixMs = 0;
            if (patient.BeddedUnixMs < 0)
                patient.BeddedUnixMs = 0;
            if (patient.LastCalcUnixMs < 0)
                patient.LastCalcUnixMs = 0;
            if (patient.Bed < 1 || patient.Bed > UnlockedBeds || used[patient.Bed])
                patient.Bed = 0;
            else
                used[patient.Bed] = true;
            if (patient.Bed != 0 && patient.LastCalcUnixMs < patient.BeddedUnixMs)
                patient.LastCalcUnixMs = patient.BeddedUnixMs;
            if (patient.Id >= _nextPatientId)
                _nextPatientId = patient.Id + 1;
        }
        int unlockedCards = (UnlockedBeds + 1) / 2;
        for (int card = 1; card <= MaxCards; card++)
        {
            if (card > unlockedCards || (_nurses[card] == 0 && _nursePersons[card] == 0))
            {
                ClearNurse(card);
                continue;
            }
            if (_nurses[card] != 0 && _nursePersons[card] != 0)
                _nursePersons[card] = 0;
            if (_nurses[card] != 0)
            {
                uint nurse = _nurses[card];
                int owner = 0;
                for (int other = 1; other < card; other++)
                {
                    if (_nurses[other] == nurse)
                    {
                        owner = other;
                        break;
                    }
                }
                if (owner != 0)
                {
                    ClearNurse(card);
                    continue;
                }
            }
            else
            {
                uint person = _nursePersons[card];
                int owner = 0;
                for (int other = 1; other < card; other++)
                {
                    if (_nursePersons[other] == person)
                    {
                        owner = other;
                        break;
                    }
                }
                if (owner != 0)
                {
                    ClearNurse(card);
                    continue;
                }
                if (_nurseSizes[card] < 1)
                    _nurseSizes[card] = HospitalMath.PersonNurseSize;
            }
            if (_nurseSizes[card] < 0)
                _nurseSizes[card] = 0;
        }
        for (int i = _growth.Count - 1; i >= 0; i--)
        {
            if (_growth[i].Id == 0 || _growth[i].Experience < 0)
                _growth.RemoveAt(i);
        }
    }

    public void Write(TextWriter writer)
    {
        if (writer == null)
            return;
        writer.WriteLine("HospitalBeds," + UnlockedBeds.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine("HospitalNextId," + _nextPatientId.ToString(CultureInfo.InvariantCulture));
        for (int i = 0; i < _grants.Count; i++)
            writer.WriteLine("HospitalGrant," + _grants[i]);
        int unlockedCards = (UnlockedBeds + 1) / 2;
        for (int card = 1; card <= unlockedCards; card++)
        {
            if (_nurses[card] != 0)
                writer.WriteLine("HospitalCardNurse," + card.ToString(CultureInfo.InvariantCulture) + "," + _nurses[card].ToString(CultureInfo.InvariantCulture) + "," + _nurseSizes[card].ToString(CultureInfo.InvariantCulture) + ",0");
            else if (_nursePersons[card] != 0)
                writer.WriteLine("HospitalCardNurse," + card.ToString(CultureInfo.InvariantCulture) + "," + _nursePersons[card].ToString(CultureInfo.InvariantCulture) + "," + _nurseSizes[card].ToString(CultureInfo.InvariantCulture) + ",1");
        }
        for (int i = 0; i < _growth.Count; i++)
        {
            if (_growth[i].Id == 0 || _growth[i].Experience <= 0)
                continue;
            writer.WriteLine("HospitalNurseExp," + (_growth[i].Person ? "1" : "0") + "," + _growth[i].Id.ToString(CultureInfo.InvariantCulture) + "," + _growth[i].Experience.ToString(CultureInfo.InvariantCulture));
        }
        for (int i = 0; i < _pending.Count; i++)
            writer.WriteLine("HospitalPending," + _pending[i].ToString(CultureInfo.InvariantCulture));
        for (int i = 0; i < _patients.Count; i++)
        {
            HospitalPatient patient = _patients[i];
            writer.WriteLine("HospitalPatient,"
                + patient.Id.ToString(CultureInfo.InvariantCulture) + ","
                + patient.CharacterId.ToString(CultureInfo.InvariantCulture) + ","
                + patient.OpaquePixels.ToString(CultureInfo.InvariantCulture) + ","
                + patient.Size.ToString(CultureInfo.InvariantCulture) + ","
                + patient.HealedPixels.ToString("G17", CultureInfo.InvariantCulture) + ","
                + patient.AdmittedUnixMs.ToString(CultureInfo.InvariantCulture) + ","
                + patient.BeddedUnixMs.ToString(CultureInfo.InvariantCulture) + ","
                + patient.LastCalcUnixMs.ToString(CultureInfo.InvariantCulture) + ","
                + patient.Bed.ToString(CultureInfo.InvariantCulture));
        }
    }

    public bool TryApply(string[] values)
    {
        if (values == null || values.Length < 1 || values[0] == null || !values[0].StartsWith("Hospital", StringComparison.Ordinal))
            return false;
        string key = values[0];
        if (key == "HospitalBeds")
        {
            int beds;
            if (values.Length >= 2 && int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out beds))
                UnlockedBeds = beds;
            return true;
        }
        if (key == "HospitalNextId")
        {
            ulong next;
            if (values.Length >= 2 && ulong.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out next) && next >= 1)
                _nextPatientId = next;
            return true;
        }
        if (key == "HospitalGrant")
        {
            if (values.Length >= 2 && !string.IsNullOrEmpty(values[1]) && !_grants.Contains(values[1]))
                _grants.Add(values[1]);
            return true;
        }
        if (key == "HospitalCardNurse" || key == "HospitalNurse")
        {
            int index;
            uint characterId;
            if (values.Length >= 3
                && int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out index)
                && uint.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out characterId))
            {
                int card = key == "HospitalCardNurse" ? index : CardOf(index);
                int kind = 0;
                if (values.Length >= 5)
                    int.TryParse(values[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out kind);
                if (card >= 1 && card <= MaxCards && _nurses[card] == 0 && _nursePersons[card] == 0)
                {
                    int size = 0;
                    if (characterId != 0 && values.Length >= 4)
                        int.TryParse(values[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out size);
                    if (kind == 1)
                    {
                        _nursePersons[card] = characterId;
                        _nurseSizes[card] = size < 1 ? HospitalMath.PersonNurseSize : size;
                    }
                    else
                    {
                        _nurses[card] = characterId;
                        _nurseSizes[card] = characterId == 0 || size < 1 ? 0 : size;
                    }
                }
            }
            return true;
        }
        if (key == "HospitalNurseExp")
        {
            int kind;
            uint id;
            int experience;
            if (values.Length >= 4
                && int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out kind)
                && uint.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                && int.TryParse(values[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out experience)
                && id != 0 && experience > 0)
            {
                bool person = kind == 1;
                int index = FindGrowth(person, id);
                if (index < 0)
                {
                    NurseGrowth growth = new NurseGrowth();
                    growth.Person = person;
                    growth.Id = id;
                    growth.Experience = experience;
                    _growth.Add(growth);
                }
                else
                    _growth[index].Experience = experience;
            }
            return true;
        }
        if (key == "HospitalPending")
        {
            uint characterId;
            if (values.Length >= 2 && uint.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out characterId) && characterId != 0)
                _pending.Add(characterId);
            return true;
        }
        if (key == "HospitalPatient")
        {
            if (values.Length < 10)
                return true;
            HospitalPatient patient = new HospitalPatient();
            ulong id;
            uint characterId;
            int opaque;
            int size;
            double healed;
            long admitted;
            long bedded;
            long last;
            int bed;
            if (!ulong.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                return true;
            if (!uint.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out characterId) || characterId == 0)
                return true;
            if (!int.TryParse(values[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out opaque))
                return true;
            if (!int.TryParse(values[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out size))
                return true;
            if (!double.TryParse(values[5], NumberStyles.Float, CultureInfo.InvariantCulture, out healed))
                return true;
            if (!long.TryParse(values[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out admitted))
                return true;
            if (!long.TryParse(values[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out bedded))
                return true;
            if (!long.TryParse(values[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out last))
                return true;
            if (!int.TryParse(values[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out bed))
                return true;
            patient.Id = id;
            patient.CharacterId = characterId;
            patient.OpaquePixels = opaque;
            patient.Size = size;
            patient.HealedPixels = healed;
            patient.AdmittedUnixMs = admitted;
            patient.BeddedUnixMs = bedded;
            patient.LastCalcUnixMs = last;
            patient.Bed = bed;
            _patients.Add(patient);
            return true;
        }
        return false;
    }

    public static bool IsComplete(HospitalPatient patient)
    {
        if (patient == null)
            return false;
        if (patient.OpaquePixels <= 0)
            return true;
        return patient.HealedPixels + 1e-6 >= patient.OpaquePixels;
    }

    void SanitizeTimes(long nowMs)
    {
        for (int i = 0; i < _patients.Count; i++)
        {
            HospitalPatient patient = _patients[i];
            if (patient.AdmittedUnixMs > nowMs)
                patient.AdmittedUnixMs = nowMs;
            if (patient.BeddedUnixMs > nowMs)
                patient.BeddedUnixMs = nowMs;
            if (patient.LastCalcUnixMs > nowMs)
                patient.LastCalcUnixMs = nowMs;
            if (patient.Bed != 0 && patient.LastCalcUnixMs < patient.BeddedUnixMs)
                patient.LastCalcUnixMs = patient.BeddedUnixMs;
        }
    }

    void FillEmptyBeds(long nowMs)
    {
        for (int bed = 1; bed <= UnlockedBeds; bed++)
        {
            if (PatientOnBed(bed) != null)
                continue;
            if (!FillBed(bed, nowMs))
                return;
        }
    }

    bool FillBed(int bed, long nowMs)
    {
        if (bed < 1 || bed > UnlockedBeds)
            return false;
        if (PatientOnBed(bed) != null)
            return false;
        HospitalPatient next = null;
        for (int i = 0; i < _patients.Count; i++)
        {
            HospitalPatient patient = _patients[i];
            if (patient.Bed != 0)
                continue;
            if (next == null || CompareQueue(patient, next) < 0)
                next = patient;
        }
        if (next == null)
            return false;
        next.Bed = bed;
        next.BeddedUnixMs = nowMs;
        next.LastCalcUnixMs = nowMs;
        return true;
    }

    static int CompareQueue(HospitalPatient a, HospitalPatient b)
    {
        int byTime = a.AdmittedUnixMs.CompareTo(b.AdmittedUnixMs);
        if (byTime != 0)
            return byTime;
        return a.Id.CompareTo(b.Id);
    }

    void AdvanceBedsTo(long untilMs)
    {
        for (int i = 0; i < _patients.Count; i++)
        {
            HospitalPatient patient = _patients[i];
            if (patient.Bed == 0)
                continue;
            if (patient.LastCalcUnixMs >= untilMs)
                continue;
            long elapsed = untilMs - patient.LastCalcUnixMs;
            if (elapsed < 0)
                elapsed = 0;
            int msPerPixel = MillisecondsPerPixel(patient);
            if (msPerPixel < 1)
                msPerPixel = 1;
            patient.HealedPixels += elapsed / (double)msPerPixel;
            if (patient.OpaquePixels > 0 && patient.HealedPixels > patient.OpaquePixels)
                patient.HealedPixels = patient.OpaquePixels;
            patient.LastCalcUnixMs = untilMs;
        }
    }

    long FinishUnixMs(HospitalPatient patient)
    {
        if (patient == null)
            return long.MaxValue;
        if (IsComplete(patient))
            return patient.LastCalcUnixMs;
        double left = patient.OpaquePixels - patient.HealedPixels;
        if (left < 0)
            left = 0;
        int msPerPixel = MillisecondsPerPixel(patient);
        if (msPerPixel < 1)
            msPerPixel = 1;
        double ms = left * msPerPixel;
        if (double.IsNaN(ms) || double.IsInfinity(ms) || ms >= long.MaxValue)
            return long.MaxValue;
        long add = (long)Math.Ceiling(ms - 1e-6);
        if (add < 1)
            add = 1;
        if (patient.LastCalcUnixMs > long.MaxValue - add)
            return long.MaxValue;
        return patient.LastCalcUnixMs + add;
    }

    void Complete(HospitalPatient patient)
    {
        if (patient == null)
            return;
        int bed = patient.Bed;
        if (patient.OpaquePixels > 0)
            patient.HealedPixels = patient.OpaquePixels;
        patient.Bed = 0;
        _patients.Remove(patient);
        _pending.Add(patient.CharacterId);
        GrantNurseExperience(bed);
    }

    void GrantNurseExperience(int bed)
    {
        int card = CardOf(bed);
        if (card < 1 || card > MaxCards)
            return;
        if (_nursePersons[card] != 0)
            AddExperience(true, _nursePersons[card], 1);
    }

    void ClearNurse(int card)
    {
        if (card < 1 || card > MaxCards)
            return;
        _nurses[card] = 0;
        _nursePersons[card] = 0;
        _nurseSizes[card] = 0;
    }

    int FindGrowth(bool person, uint id)
    {
        for (int i = 0; i < _growth.Count; i++)
        {
            if (_growth[i].Person == person && _growth[i].Id == id)
                return i;
        }
        return -1;
    }

    ulong PatientIdOnBed(int bed)
    {
        HospitalPatient patient = PatientOnBed(bed);
        if (patient == null)
            return 0;
        return patient.Id;
    }

    HospitalPatient FindPatient(ulong id)
    {
        if (id == 0)
            return null;
        for (int i = 0; i < _patients.Count; i++)
        {
            if (_patients[i].Id == id)
                return _patients[i];
        }
        return null;
    }

    bool TapPatient(HospitalPatient patient, long nowMs)
    {
        if (patient == null || patient.Bed == 0 || IsComplete(patient))
            return false;
        int msPerPixel = MillisecondsPerPixel(patient);
        if (msPerPixel < 1)
            msPerPixel = 1;
        patient.HealedPixels += HospitalMath.SpeedUpMilliseconds / (double)msPerPixel;
        if (patient.OpaquePixels > 0 && patient.HealedPixels > patient.OpaquePixels)
            patient.HealedPixels = patient.OpaquePixels;
        patient.LastCalcUnixMs = nowMs;
        return true;
    }

    int FlushPending(Func<uint, bool> tryReturn)
    {
        if (tryReturn == null || _pending.Count == 0)
            return 0;
        int returned = 0;
        for (int i = 0; i < _pending.Count;)
        {
            if (!tryReturn(_pending[i]))
            {
                i++;
                continue;
            }
            _pending.RemoveAt(i);
            returned++;
        }
        return returned;
    }
}
