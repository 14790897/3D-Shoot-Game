[Bullet] CCD 命中: Bullet 层=Default 点=(14.86, 1.09, 12.61) 法线=(0.96, 0.00, 0.29) 距离=1.568
[Bullet] 碰撞命中: Bullet 层=Default 点=(14.86, 1.09, 12.61) 法线=(0.96, 0.00, 0.29)


你贴的日志显示子弹在用 CCD 和碰撞都“命中了 Bullet（子弹）”，说明子弹在途中撞到自己或其它子弹，提前触发命中。已修正如下，并说明哪里“导入/调用”了 Bullet。

修正内容

过滤子弹与子弹的命中
Bullet.FixedUpdate 改为 SphereCastAll，按距离排序，跳过自身和任何带 Bullet 组件的碰撞体，只处理第一个有效命中。
Bullet.OnCollisionEnter 同样跳过与其它子弹的碰撞。
代码：Assets/Scripts/Bullet.cs
传递正确的射线层
Weapon 在创建子弹时把“用于 CCD 的层掩码”传给 Bullet（排除了 Player；如果你建了 Projectile 层也一起排除），避免 CCD 打到不该打的层。
代码：Assets/Scripts/Weapon.cs
你需要做的简单设置

如果项目里有“Projectile”层，建议新建并用它；这样可以在 Layer 矩阵里让 Projectile 不和自己相撞。我也可以在 Weapon 里把生成的子弹自动放到这个层并调用 Physics.IgnoreLayerCollision(Projectle, Projectile)；如果你要，我再加这一行初始化。
Bullet 是哪里“导入/调用”的

不需要 import。Bullet 是你工程内脚本（Assets/Scripts/Bullet.cs），由 Weapon 在发射时动态挂上并初始化。
调用链：
Weapon.Update 检输入 → TryFire
TryFire 中若勾选 useProjectile → 计算方向后调用 LaunchProjectile
LaunchProjectile 创建子弹物体，AddComponent<Bullet>() 并调用 b.Init(...)
接下来怎么验证

在 Inspector 勾上 Weapon.logHits，开火几枪。
正常情况下，日志不再出现“命中 Bullet”，而是显示命中 Ground/Enemy 等。
如果你愿意，我可以再把生成的子弹自动放到“Projectile”层，并在 Awake 里加 Physics.IgnoreLayerCollision(Projectle, Projectile) 来彻底避免子弹互撞。要我加上吗？