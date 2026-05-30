#!/usr/bin/env python3
"""
Appointment Management System - Project Manager
Script để quản lý toàn bộ dự án (backend + frontend)
"""

import os
import sys
import subprocess
import argparse
import threading
import time
import platform
from pathlib import Path
from typing import Optional

class ProjectManager:
    def __init__(self):
        self.root_dir = Path(__file__).parent
        self.app_dir = self.root_dir / "application"
        self.static_dir = self.root_dir / "static"
        self.db_dir = self.root_dir / "database"
        self.gateway_dir = self.root_dir / "gateway"
        self.backend_process = None
        self.frontend_process = None
        self.gateway_process = None
        
        # Load .env file
        self.load_env()

        # Detect npm command based on OS
        self.npm_cmd = "npm.cmd" if platform.system() == "Windows" else "npm"
        self.dotnet_cmd = "dotnet.exe" if platform.system() == "Windows" else "dotnet"

    def load_env(self):
        """Nạp các biến từ file .env vào môi trường"""
        env_path = self.root_dir / ".env"
        if env_path.exists():
            print("📝 Đang nạp cấu hình từ file .env...")
            with open(env_path, "r", encoding="utf-8") as f:
                for line in f:
                    line = line.strip()
                    if not line or line.startswith("#"):
                        continue
                    if "=" in line:
                        key, value = line.split("=", 1)
                        os.environ[key.strip()] = value.strip()

    def run_backend(self, port: int = 5000):
        """Chạy backend (.NET)"""
        print(f"🚀 Khởi động Backend trên port {port}...")
        os.chdir(self.app_dir)
        try:
            subprocess.run([self.dotnet_cmd, "run"], check=True)
        except FileNotFoundError:
            print("❌ Lỗi: dotnet không được tìm thấy. Vui lòng cài đặt .NET SDK")
            print("   Download: https://dotnet.microsoft.com/en-us/download/dotnet/8.0")
            sys.exit(1)
        except KeyboardInterrupt:
            print("\n⏹️  Backend đã dừng")

    def run_gateway(self, port: int = 5000):
        """Chạy API Gateway (Ocelot)"""
        print(f"🌐 Khởi động API Gateway trên port {port}...")
        os.chdir(self.gateway_dir)
        try:
            subprocess.run([self.dotnet_cmd, "run"], check=True)
        except FileNotFoundError:
            print("❌ Lỗi: dotnet không được tìm thấy.")
            sys.exit(1)
        except KeyboardInterrupt:
            print("\n⏹️  Gateway đã dừng")

    def run_frontend(self, port: int = 5173):
        """Chạy frontend (Vue.js)"""
        print(f"🎨 Khởi động Frontend trên port {port}...")
        os.chdir(self.static_dir)
        try:
            subprocess.run([self.npm_cmd, "run", "dev"], check=True)
        except FileNotFoundError:
            print("❌ Lỗi: npm không được tìm thấy. Vui lòng cài đặt Node.js")
            print("   Download: https://nodejs.org/")
            sys.exit(1)
        except KeyboardInterrupt:
            print("\n⏹️  Frontend đã dừng")
    
    def run_backend_process(self):
        """Chạy backend trong background (subprocess)"""
        try:
            self.backend_process = subprocess.Popen(
                [self.dotnet_cmd, "run"],
                cwd=str(self.app_dir),
                shell=True if platform.system() == "Windows" else False
            )
            self.backend_process.wait()
        except FileNotFoundError:
            print("❌ Lỗi: dotnet không được tìm thấy. Vui lòng cài đặt .NET SDK")
            print("   Download: https://dotnet.microsoft.com/en-us/download/dotnet/8.0")
            sys.exit(1)
        except Exception as e:
            print(f"❌ Lỗi chạy backend: {e}")
    
    def run_gateway_process(self):
        """Chạy gateway trong background (subprocess)"""
        try:
            self.gateway_process = subprocess.Popen(
                [self.dotnet_cmd, "run"],
                cwd=str(self.gateway_dir),
                shell=True if platform.system() == "Windows" else False
            )
            self.gateway_process.wait()
        except FileNotFoundError:
            print("❌ Lỗi: dotnet không được tìm thấy. Vui lòng cài đặt .NET SDK")
            sys.exit(1)
        except Exception as e:
            print(f"❌ Lỗi chạy gateway: {e}")

    def run_frontend_process(self):
        """Chạy frontend trong background (subprocess)"""
        try:
            self.frontend_process = subprocess.Popen(
                [self.npm_cmd, "run", "dev"],
                cwd=str(self.static_dir),
                shell=True if platform.system() == "Windows" else False
            )
            self.frontend_process.wait()
        except FileNotFoundError:
            print("❌ Lỗi: npm không được tìm thấy. Vui lòng cài đặt Node.js")
            print("   Download: https://nodejs.org/")
            sys.exit(1)
        except Exception as e:
            print(f"❌ Lỗi chạy frontend: {e}")

    def setup_backend(self):
        """Setup backend"""
        print("📦 Cài đặt backend...")
        os.chdir(self.app_dir)
        
        # Restore dependencies
        print("  • Khôi phục dependencies...")
        subprocess.run([self.dotnet_cmd, "restore"], check=True)
        
        print("✅ Backend đã được cài đặt")
        os.chdir(self.root_dir)

    def setup_gateway(self):
        """Setup API Gateway"""
        print("🌐 Cài đặt API Gateway...")
        os.chdir(self.gateway_dir)
        subprocess.run([self.dotnet_cmd, "restore"], check=True)
        print("✅ API Gateway đã được cài đặt")
        os.chdir(self.root_dir)

    def setup_frontend(self):
        """Setup frontend"""
        print("📦 Cài đặt frontend...")
        os.chdir(self.static_dir)
        
        # Install dependencies
        print("  • Cài đặt dependencies...")
        subprocess.run([self.npm_cmd, "install"], check=True)
        
        print("✅ Frontend đã được cài đặt")
        os.chdir(self.root_dir)

    def build_frontend(self):
        """Build frontend cho production"""
        print("🔨 Build frontend...")
        os.chdir(self.static_dir)
        subprocess.run([self.npm_cmd, "run", "build"], check=True)
        print("✅ Build hoàn thành")
        os.chdir(self.root_dir)

    def install_dependencies(self):
        """Cài đặt tất cả dependencies"""
        self.setup_backend()
        self.setup_gateway()
        self.setup_frontend()
        print("✅ Tất cả dependencies đã được cài đặt")

    def run_all(self):
        """Chạy cả backend và frontend cùng lúc"""
        print("\n" + "="*60)
        print("🚀 Khởi động toàn bộ dự án...")
        print("="*60)
        print(f"  📍  Backend: http://localhost:5150")
        print(f"  🎨  Frontend: http://localhost:3000")
        print(f"  🌐  Gateway: python manage.py start:gateway (chạy riêng nếu cần)")
        print(f"  Nhấn Ctrl+C để dừng tất cả")
        print("="*60 + "\n")
        
        # Create threads để chạy backend + frontend
        backend_thread = threading.Thread(target=self.run_backend_process, daemon=False)
        frontend_thread = threading.Thread(target=self.run_frontend_process, daemon=False)
        
        try:
            backend_thread.start()
            print("✅ Backend đã khởi động\n")
            
            # Đợi 3 giây trước khi khởi động frontend
            time.sleep(2)
            
            frontend_thread.start()
            print("✅ Frontend đã khởi động\n")
            
            # Chờ cả hai threads hoàn thành
            backend_thread.join()
            frontend_thread.join()
            
        except KeyboardInterrupt:
            print("\n\n⏹️  Dừng tất cả processes...")
            
            # Terminate processes
            if self.backend_process:
                self.backend_process.terminate()
                try:
                    self.backend_process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    self.backend_process.kill()
            
            if self.gateway_process:
                self.gateway_process.terminate()
                try:
                    self.gateway_process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    self.gateway_process.kill()

            if self.frontend_process:
                self.frontend_process.terminate()
                try:
                    self.frontend_process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    self.frontend_process.kill()
            
            print("✅ Tất cả processes đã dừng\n")
            sys.exit(0)

def main():
    parser = argparse.ArgumentParser(
        description="Appointment Management System - Project Manager"
    )
    
    subparsers = parser.add_subparsers(dest="command", help="Command")
    
    # Setup commands
    subparsers.add_parser("setup", help="Cài đặt backend + frontend")
    subparsers.add_parser("setup:backend", help="Cài đặt backend")
    subparsers.add_parser("setup:frontend", help="Cài đặt frontend")
    subparsers.add_parser("setup:gateway", help="Cài đặt API Gateway")
    
    # Run commands
    subparsers.add_parser("run", help="Chạy backend + frontend cùng lúc ⭐")
    subparsers.add_parser("start", help="Hướng dẫn chạy backend + frontend")
    subparsers.add_parser("start:backend", help="Chạy backend riêng")
    subparsers.add_parser("start:frontend", help="Chạy frontend riêng")
    subparsers.add_parser("start:gateway", help="Chạy API Gateway riêng")
    
    # Build commands
    subparsers.add_parser("build", help="Build frontend")
    
    # Dependencies
    subparsers.add_parser("install", help="Cài đặt tất cả dependencies")
    
    args = parser.parse_args()
    
    manager = ProjectManager()
    
    if args.command == "setup":
        manager.install_dependencies()
    elif args.command == "setup:backend":
        manager.setup_backend()
    elif args.command == "setup:frontend":
        manager.setup_frontend()
    elif args.command == "setup:gateway":
        manager.setup_gateway()
    elif args.command == "run":
        manager.run_all()
    elif args.command == "start":
        manager.run_all()
    elif args.command == "start:backend":
        manager.run_backend()
    elif args.command == "start:frontend":
        manager.run_frontend()
    elif args.command == "start:gateway":
        manager.run_gateway()
    elif args.command == "build":
        manager.build_frontend()
    elif args.command == "install":
        manager.install_dependencies()
    else:
        parser.print_help()

if __name__ == "__main__":
    main()
