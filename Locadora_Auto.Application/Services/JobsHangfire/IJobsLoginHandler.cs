namespace Locadora_Auto.Application.Services.JobsHangfire
{
    public interface IJobsLoginHandler
    {
        void SetAdminTokenInterno();
        void SetAdminTokenExterno();
    }
}
