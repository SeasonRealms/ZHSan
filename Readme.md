
素材统一采用桌面版，在Resources\Raw\Content下，原有的移动压缩版TexturesAlpha已去掉。

字体采用MSDF预编译与缺失情况时的动态生成，这提供了性能和支持的最大化，缺点是换了字体需要重新预编译。

采用了材质动态加载技术，在加载未完成时跳过显示，而非等待加载完成，这消除了卡顿。

性能还可以再优化（采用类似MonoGame的合并批处理），只是当前似乎性能已经足够。

基于MAUI单项目框架，一个项目同时支持Windows/Linux/Android/MacCatalyst/iOS，注意编译调试目标与.net Framework的对应关系。

Windows需要显卡支持DirectX12, Linux/Android需要Vulkan 1.1，Android要求Android10(API 29)及以上。

由Deepseek V4.1 flash基于SeasonXNA移植（提供了主要的MonoGame仿真层，原有XNA命名空间可以保留），当前测试通过Windows/Android，Linux/MacCatalyst/iOS未测试。

Android publish keystore

Location: Zhsan.keystore
Password: Zhsan123
Alias: Zhsan
Key Password: Zhsan123
