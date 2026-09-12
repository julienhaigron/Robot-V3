using UnityEngine;
using System;

public class HighlightMainComponentsTask : Task
{
    private readonly bool isHighlighted;

    public HighlightMainComponentsTask ( string _description, Func<TaskManager.TaskContext, bool> _startPredicate, bool _isHighlighted )
        : base(_description, _startPredicate)
    {
        this.isHighlighted = _isHighlighted;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        base.OnStart(_context);

        EntityConfigPanel entityConfigPanel = _context.UI.GetPanel<EntityConfigPanel>();

        if (entityConfigPanel != null && entityConfigPanel.InventoryGrid != null)
            entityConfigPanel.InventoryGrid.SetMainComponentsHighlighted(isHighlighted);
        else
            Debug.LogWarning("No EntityConfigPanel inventory grid found, " + Description + " did nothing");

        Complete();
    }
}
