import os
import shutil

import pandas as pd


def convert_excel_to_txt(folder_path='.'):
    # ✅ 关键修复：将 '.' 转换为脚本所在目录的绝对路径
    if folder_path == '.':
        folder_path = os.path.dirname(os.path.abspath(__file__))
    
    print(f"【调试】当前工作目录: {os.getcwd()}")
    print(f"【调试】遍历起始目录: {folder_path}\n")
    
    count = 0
    target_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), '../Assets/GameMain/DataTables')
    target_dir = os.path.abspath(target_dir)
    # ... 后续代码不变

    # 确保目标目录存在
    os.makedirs(target_dir, exist_ok=True)

    for root, _, files in os.walk(folder_path):
        for file_name in files:
            if not file_name.lower().endswith(('.xlsx', '.xls')):
                continue
            # 跳过 Excel 临时锁文件
            if file_name.startswith('~$'):
                continue

            file_path = os.path.join(root, file_name)
            base_name = os.path.splitext(file_path)[0]
            output_file = base_name + '.txt'

            print(f"正在处理: {file_path}...")

            try:
                # 保留单元格中的 "None" 文本，避免被 pandas 当作缺失值转为空
                df = pd.read_excel(file_path, header=None, keep_default_na=False)

                df.to_csv(output_file, sep='\t', index=False, header=False, encoding='utf-8')

                # 复制文件到目标目录
                target_file = os.path.join(target_dir, os.path.basename(output_file))
                shutil.copy2(output_file, target_file)

                print(f"成功转换 -> {output_file}")
                print(f"已复制到 -> {target_file}")
                count += 1

            except Exception as e:
                print(f"处理 {file_path} 时出错: {e}")

    print(f"\n任务完成！共转换了 {count} 个文件。")


if __name__ == "__main__":
    convert_excel_to_txt('.')

    print("\n" + "=" * 30)
    input("按回车键(Enter)退出程序...")
