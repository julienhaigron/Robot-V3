using System.Collections.Generic;

public readonly struct ActionExchange
{
	public enum Tone { Up, Down, Neutral }
	public enum TileClaim { None, Hold, Yield, CoinFlip }

	public readonly struct Row
	{
		public readonly Tone tone;
		public readonly string text;

		public Row ( Tone _tone, string _text )
		{
			tone = _tone;
			text = _text;
		}
	}

	public readonly EntityActionData data;
	public readonly bool isAttack;
	public readonly bool movesItsUnit;
	public readonly bool hasArea;
	public readonly List<EntityActionData.ActionType> intercepts;
	public readonly List<EntityActionData.ActionType> aimsAtArrival;
	public readonly List<EntityActionData.ActionSubType> interceptedBy;
	public readonly TileClaim claim;

	private ActionExchange ( EntityActionData _data )
	{
		data = _data;
		isAttack = _data.GetMainActionType() == EntityActionData.MainActionType.Attack;
		movesItsUnit = _data.MovesItsUnit;
		hasArea = _data.aoeType != EntityActionData.AOEType.Noone && _data.aoeType != EntityActionData.AOEType.Chain;
		intercepts = new();
		aimsAtArrival = new();
		interceptedBy = new();
		claim = TileClaim.None;

		List<EntityActionData> classes = new();
		List<EntityActionData> movers = new();
		EntityActionData walker = null;
		foreach (EntityActionData other in GameAssets.current.game.entityActionsData.Values)
		{
			if (other == null)
				continue;

			if (other.GetMainActionType() == EntityActionData.MainActionType.Attack && !classes.Exists(c => c.type == other.type && c.subType == other.subType))
				classes.Add(other);
			if (other.MovesItsUnit && !movers.Exists(m => m.type == other.type))
				movers.Add(other);
			if (walker == null && other.codeType == EntityActionData.ActionCodeType.TargetTileMove)
				walker = other;
		}
		classes.Sort(( a, b ) => ClassOrder(a).CompareTo(ClassOrder(b)));
		movers.Sort(( a, b ) => MoverOrder(a.type).CompareTo(MoverOrder(b.type)));

		if (isAttack)
		{
			foreach (EntityActionData mover in movers)
				(EntityActionData.PFC(_data, mover) == EntityActionData.PFCResultType.FirstWins ? intercepts : aimsAtArrival).Add(mover.type);
		}

		if (movesItsUnit)
		{
			foreach (EntityActionData attackClass in classes)
				if (EntityActionData.PFC(attackClass, _data) == EntityActionData.PFCResultType.FirstWins)
					interceptedBy.Add(attackClass.subType);
		}

		if (_data.ClaimsTargetTiles && walker != null)
		{
			EntityActionData.PFCResultType result = EntityActionData.PFC(_data, walker);
			claim = result == EntityActionData.PFCResultType.FirstWins ? TileClaim.Hold
				: result == EntityActionData.PFCResultType.SecondWins ? TileClaim.Yield
				: TileClaim.CoinFlip;
		}
	}

	public static bool TryGet ( EntityActionData _data, out ActionExchange _exchange )
	{
		_exchange = _data == null || _data.IsRotation ? default : new ActionExchange(_data);
		return _exchange.data != null && (_exchange.isAttack || _exchange.movesItsUnit || _exchange.claim != TileClaim.None);
	}

	public bool HasClass => isAttack || movesItsUnit;

	public string ClassName => isAttack ? data.subType.GetExchangeClassName() : movesItsUnit ? data.type.GetExchangeClassName() : "";

	public string ClassTypeLabel => isAttack ? data.type.GetLocalizedTitle() : "";

	public void GetRows ( List<Row> _rows )
	{
		_rows.Clear();
		LocalizationManager localization = LocalizationManager.Instance;

		if (isAttack)
			_rows.Add(intercepts.Count > 0
				? new Row(Tone.Up, string.Format(localization.Get(LocalizationKey.exchange_row_intercepts), JoinMovers(intercepts)))
				: new Row(Tone.Neutral, localization.Get(LocalizationKey.exchange_row_no_intercept)));

		if (movesItsUnit)
			_rows.Add(interceptedBy.Count > 0
				? new Row(Tone.Down, string.Format(localization.Get(LocalizationKey.exchange_row_intercepted_by), JoinClasses(interceptedBy)))
				: new Row(Tone.Up, localization.Get(LocalizationKey.exchange_row_never_intercepted)));

		switch (claim)
		{
			case TileClaim.Hold: _rows.Add(new Row(Tone.Up, localization.Get(LocalizationKey.exchange_row_claim_hold))); break;
			case TileClaim.Yield: _rows.Add(new Row(Tone.Down, localization.Get(LocalizationKey.exchange_row_claim_yield))); break;
			case TileClaim.CoinFlip: _rows.Add(new Row(Tone.Neutral, localization.Get(LocalizationKey.exchange_row_claim_coin))); break;
		}
	}

	public string GetDescription ()
	{
		LocalizationManager localization = LocalizationManager.Instance;
		List<string> sentences = new();

		if (isAttack)
		{
			string reach = localization.Get(hasArea ? LocalizationKey.exchange_reach_area : LocalizationKey.exchange_reach_range);
			if (intercepts.Count > 0)
				sentences.Add(string.Format(localization.Get(LocalizationKey.exchange_text_intercepts), JoinVerbs(intercepts)));
			if (aimsAtArrival.Count > 0)
				sentences.Add(string.Format(localization.Get(intercepts.Count > 0 ? LocalizationKey.exchange_text_misses_after : LocalizationKey.exchange_text_misses)
					, JoinVerbs(aimsAtArrival), reach));
		}

		if (movesItsUnit)
			sentences.Add(interceptedBy.Count > 0
				? string.Format(localization.Get(LocalizationKey.exchange_text_mover), JoinClasses(interceptedBy)
					, isAttack ? "" : localization.Get(LocalizationKey.exchange_text_mover_dodge))
				: localization.Get(LocalizationKey.exchange_text_mover_never));

		switch (claim)
		{
			case TileClaim.Hold: sentences.Add(localization.Get(LocalizationKey.exchange_text_claim_hold)); break;
			case TileClaim.Yield: sentences.Add(localization.Get(LocalizationKey.exchange_text_claim_yield)); break;
			case TileClaim.CoinFlip: sentences.Add(localization.Get(LocalizationKey.exchange_text_claim_coin)); break;
		}

		return string.Join(" ", sentences);
	}

	private static int ClassOrder ( EntityActionData _data )
	{
		return MoverOrder(_data.type) * 100 + (int)_data.subType;
	}

	private static int MoverOrder ( EntityActionData.ActionType _type )
	{
		return _type switch
		{
			EntityActionData.ActionType.Movement => 0,
			EntityActionData.ActionType.MeleeAttack => 1,
			EntityActionData.ActionType.DistanceAttack => 2,
			_ => 3
		};
	}

	private static string JoinMovers ( List<EntityActionData.ActionType> _types )
	{
		List<string> names = new(_types.Count);
		foreach (EntityActionData.ActionType type in _types)
			names.Add(type.GetMoversLabel());
		return JoinWith(names, LocalizationKey.tooltip_and);
	}

	private static string JoinVerbs ( List<EntityActionData.ActionType> _types )
	{
		List<string> verbs = new(_types.Count);
		foreach (EntityActionData.ActionType type in _types)
			verbs.Add(type.GetMoverVerb());
		return JoinWith(verbs, LocalizationKey.tooltip_or);
	}

	private static string JoinClasses ( List<EntityActionData.ActionSubType> _classes )
	{
		List<string> names = new(_classes.Count);
		foreach (EntityActionData.ActionSubType subType in _classes)
			names.Add(subType.GetExchangeClassName());
		return JoinWith(names, LocalizationKey.tooltip_and);
	}

	private static string JoinWith ( List<string> _items, LocalizationKey _lastSeparator )
	{
		if (_items.Count <= 1)
			return _items.Count == 0 ? "" : _items[0];

		return string.Join(", ", _items.GetRange(0, _items.Count - 1)) + LocalizationManager.Instance.Get(_lastSeparator) + _items[^1];
	}
}
