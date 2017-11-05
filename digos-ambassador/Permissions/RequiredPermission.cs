namespace DIGOS.Ambassador.Permissions
{
	public class RequiredPermission
	{
		public Permission Permission { get; set; }

		public PermissionTarget Target { get; set; }

		public RequiredPermission()
		{
		}

		public RequiredPermission(Permission permission, PermissionTarget target)
		{
			this.Permission = permission;
			this.Target = target;
		}
	}
}
