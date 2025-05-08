# Sử dụng image chính thức của GCC
FROM gcc:latest

# Cài đặt build-essential và các công cụ cần thiết
RUN apt-get update && apt-get install -y \
    build-essential \
    g++ \
    && rm -rf /var/lib/apt/lists/*

# Thiết lập thư mục làm việc trong container
WORKDIR /workspace

# Lệnh mặc định khi chạy container (nếu không cung cấp lệnh gì sẽ chạy bash)
CMD ["bash"]
