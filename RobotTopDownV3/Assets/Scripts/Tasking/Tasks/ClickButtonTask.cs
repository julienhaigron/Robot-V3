using UnityEngine;

public class ClickButtonTask : Task
{
    private readonly BaseButton button;

    public ClickButtonTask ( string _description, bool _canBeSkipped,  BaseButton _button ) 
        : base(_description, _canBeSkipped)
    {
        this.button = _button;
    }

    protected override void OnStart ( TaskManager.TaskContext _context )
    {
        button.onClick += OnClicked;
    }

    private void OnClicked ()
    {
        Complete();
    }

    protected override void OnComplete ()
    {
        button.onClick -= OnClicked;
    }
}