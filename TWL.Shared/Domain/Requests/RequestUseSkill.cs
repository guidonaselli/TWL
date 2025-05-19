namespace TWL.Shared.Domain.Requests;

public class RequestUseSkill
{
    public int PlayerId { get; set; }
    public int TargetId { get; set; }
    public int SkillId { get; set; }
}