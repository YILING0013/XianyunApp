import os
import json

# 设置目录路径
directory_path = './'  # 这里可以指定你的目录路径

# 遍历目录下的所有文件
for filename in os.listdir(directory_path):
    if filename.endswith('.json'):  # 检查是否为JSON文件
        file_path = os.path.join(directory_path, filename)
        
        # 获取文件名，移除.json后缀
        file_name_without_extension = filename.rsplit('.', 1)[0]
        
        # 打开并读取JSON文件
        with open(file_path, 'r', encoding='utf-8') as file:
            try:
                data = json.load(file)
                
                # 打印文件名和文件内的主键
                if isinstance(data, dict):
                    keys = list(data.keys())
                    print(f"{file_name_without_extension}: {', '.join(keys)}")
                else:
                    print(f"{file_name_without_extension}: 数据格式不是字典类型")
            
            except json.JSONDecodeError:
                print(f"{file_name_without_extension}: 无法解析文件")
