using System.Collections.Generic;
using UnityEngine;

public class LogCard
{
	public enum Kind { HitRoll, StatusRoll, Damage, Status, Death, Sabotage }
	public enum Tone { Neutral, Up, Down }
	public enum ColumnKind { Neutral, Attack, Defense }

	public readonly struct Row
	{
		public readonly string label;
		public readonly string value;

		public Row ( string _label, string _value )
		{
			label = _label;
			value = _value;
		}
	}

	public class Column
	{
		public readonly ColumnKind kind;
		public readonly string title;
		public readonly string total;
		public readonly List<Row> rows = new();

		public Column ( ColumnKind _kind, string _title, float _total )
		{
			kind = _kind;
			title = _title;
			total = _total.ToPercentText();
		}

		public void Add ( string _label, float _ratio )
		{
			int percent = _ratio.ToPercent();
			if (percent != 0)
				rows.Add(new Row(_label, Signed(percent)));
		}

		public void Add ( LocalizationKey _label, float _ratio )
		{
			Add(LocalizationManager.Instance.Get(_label), _ratio);
		}
	}

	public class DamageStep
	{
		public string amount;
		public string label;
		public string value;
		public Tone tone;
	}

	public class DamageLine
	{
		public readonly WeaponEquipmentData.DamageType type;
		public readonly int baseValue;
		public readonly List<DamageStep> steps = new();
		public int final;

		public DamageLine ( WeaponEquipmentData.DamageType _type, int _baseValue )
		{
			type = _type;
			baseValue = _baseValue;
			final = _baseValue;
		}

		public void AddFactor ( float _factor, float _value )
		{
			if (Mathf.Approximately(_factor, 1f))
				return;

			steps.Add(new DamageStep { amount = "x" + _factor.ToString("0.##"), label = LocalizationManager.Instance.Get(LocalizationKey.log_card_step_action), value = _value.ToString("0.#"), tone = _factor > 1f ? Tone.Up : Tone.Down });
		}

		public void AddPercent ( string _label, float _ratio, float _value )
		{
			int percent = _ratio.ToPercent();
			if (percent == 0)
				return;

			steps.Add(new DamageStep { amount = string.Format(LocalizationManager.Instance.Get(LocalizationKey.tooltip_percent), Signed(percent)), label = _label, value = _value.ToString("0.#"), tone = percent > 0 ? Tone.Up : Tone.Down });
		}

		public void AddPercent ( LocalizationKey _label, float _ratio, float _value )
		{
			AddPercent(LocalizationManager.Instance.Get(_label), _ratio, _value);
		}
	}

	public readonly Kind kind;
	public readonly string title;
	public string actor;
	public string target;
	public Sprite icon;
	public Color? accent;

	public string verdict;
	public Tone verdictTone;
	public WeaponEquipmentData.DamageType? verdictDamageType;
	public bool isVerdictValue;

	public bool hasRoll;
	public float chance;
	public float? roll;
	public string equation;
	public readonly List<Column> columns = new();

	public readonly List<DamageLine> damages = new();

	public AEntityStatus status;
	public bool showDuration;
	public int duration;
	public int remaining;

	public readonly List<Row> facts = new();
	public bool hasHealth;
	public int health;
	public int maxHealth;

	public string description;

	public LogCard ( Kind _kind, string _title )
	{
		kind = _kind;
		title = _title;
	}

	public static LogCard Roll ( Kind _kind, LocalizationKey _title, string _actor, string _target, float _score, float _roll, bool _isSuccess, LocalizationKey _success, LocalizationKey _failure )
	{
		bool isCertain = _score >= 1f;
		LocalizationManager localization = LocalizationManager.Instance;
		return new LogCard(_kind, localization.Get(_title))
		{
			actor = _actor,
			target = _target,
			hasRoll = true,
			chance = Mathf.Clamp01(_score),
			roll = isCertain ? null : _roll,
			verdict = localization.Get(isCertain ? LocalizationKey.log_card_guaranteed : _isSuccess ? _success : _failure),
			verdictTone = _isSuccess ? Tone.Up : Tone.Down
		};
	}

	public static LogCard Status ( AEntityStatus _status, string _unitName, int _duration, int _remaining )
	{
		return new LogCard(Kind.Status, _status.GetLocalizedName())
		{
			actor = _unitName,
			icon = _status.icon,
			accent = _status.color,
			status = _status,
			showDuration = true,
			duration = _duration,
			remaining = _remaining,
			description = _status.GetLocalizedDescription()
		};
	}

	public Column AddColumn ( ColumnKind _kind, LocalizationKey _title, float _total )
	{
		Column column = new(_kind, LocalizationManager.Instance.Get(_title), _total);
		columns.Add(column);
		return column;
	}

	public DamageLine AddDamage ( WeaponEquipmentData.DamageType _type, int _baseValue )
	{
		DamageLine line = new(_type, _baseValue);
		damages.Add(line);
		return line;
	}

	public void AddFact ( LocalizationKey _label, string _value )
	{
		facts.Add(new Row(LocalizationManager.Instance.Get(_label), _value));
	}

	public void SetDamageVerdict ( Dictionary<WeaponEquipmentData.DamageType, int> _damages )
	{
		verdict = Sum(_damages, out verdictDamageType).ToString();
		isVerdictValue = true;
	}

	public static int Sum ( Dictionary<WeaponEquipmentData.DamageType, int> _damages, out WeaponEquipmentData.DamageType? _main )
	{
		int total = 0;
		int best = int.MinValue;
		_main = null;
		foreach (KeyValuePair<WeaponEquipmentData.DamageType, int> pair in _damages)
		{
			total += pair.Value;
			if (pair.Value <= best)
				continue;

			best = pair.Value;
			_main = pair.Key;
		}

		return total;
	}

	public void SetHealth ( int _health, int _maxHealth )
	{
		hasHealth = true;
		health = _health;
		maxHealth = _maxHealth;
		AddFact(LocalizationKey.log_card_health, _health + " / " + _maxHealth);
	}

	public static string Signed ( int _value )
	{
		return (_value > 0 ? "+" : "-") + Mathf.Abs(_value);
	}
}
