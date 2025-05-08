#!/bin/bash

# Tên tệp mã nguồn (ví dụ: test.cpp hoặc test.py)
input_file=$1
language=$2

# Kiểm tra loại ngôn ngữ và biên dịch/run
if [ "$language" == "cpp" ]; then
    # Biên dịch C++
    g++ -o output_program "$input_file"
    if [ $? -eq 0 ]; then
        # Chạy chương trình trong sandbox
        unshare -f -u -n -p --mount-proc ./output_program
    else
        echo "Biên dịch C++ thất bại"
    fi
elif [ "$language" == "python" ]; then
    # Chạy mã Python trong sandbox
    unshare -f -u -n -p --mount-proc python3 "$input_file"
else
    echo "Ngôn ngữ không hợp lệ"
fi
