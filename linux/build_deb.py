import os
import tarfile
import io
import time

def build_ar_header(name, size, mode=0o100644, mtime=None):
    if mtime is None:
        mtime = int(time.time())
    header = bytearray(60)
    # File name (16 bytes)
    name_bytes = name.encode('ascii')
    header[0:len(name_bytes)] = name_bytes
    # mtime (12 bytes)
    mtime_bytes = str(mtime).encode('ascii')
    header[16:16+len(mtime_bytes)] = mtime_bytes
    # owner / group (6 + 6 bytes)
    header[28:29] = b'0'
    header[34:35] = b'0'
    # mode (8 bytes octal)
    mode_str = f"{mode:o}".encode('ascii')
    header[40:40+len(mode_str)] = mode_str
    # size (10 bytes)
    size_str = str(size).encode('ascii')
    header[48:48+len(size_str)] = size_str
    # end of header (2 bytes: `\n)
    header[58:60] = b'\x60\x0a'
    return bytes(header)

def make_tar_gz(source_dir, is_control=False):
    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w:gz") as tar:
        for root, dirs, files in os.walk(source_dir):
            for f in files:
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, source_dir).replace('\\', '/')
                if is_control:
                    tar_name = f"./{rel_path}"
                else:
                    tar_name = f"./{rel_path}"

                # Ensure executable permissions for scripts
                tarinfo = tar.gettarinfo(full_path, arcname=tar_name)
                tarinfo.uid = 0
                tarinfo.gid = 0
                tarinfo.uname = "root"
                tarinfo.gname = "root"
                
                if "bin/" in rel_path or "DEBIAN/postinst" in full_path or "DEBIAN/prerm" in full_path:
                    tarinfo.mode = 0o755
                elif "sudoers.d" in rel_path:
                    tarinfo.mode = 0o440
                else:
                    tarinfo.mode = 0o644

                with open(full_path, "rb") as fp:
                    tar.addfile(tarinfo, fp)
    return buf.getvalue()

def build_deb(pkg_root, output_deb):
    debian_binary = b"2.0\n"
    control_dir = os.path.join(pkg_root, "DEBIAN")
    
    # Create temporary root for data
    control_data = make_tar_gz(control_dir, is_control=True)
    
    # Prepare data tar.gz excluding DEBIAN
    data_buf = io.BytesIO()
    with tarfile.open(fileobj=data_buf, mode="w:gz") as tar:
        for item in ["etc", "usr"]:
            item_path = os.path.join(pkg_root, item)
            if os.path.exists(item_path):
                for root, dirs, files in os.walk(item_path):
                    for f in files:
                        full_path = os.path.join(root, f)
                        rel_path = os.path.relpath(full_path, pkg_root).replace('\\', '/')
                        tarinfo = tar.gettarinfo(full_path, arcname=f"./{rel_path}")
                        tarinfo.uid = 0
                        tarinfo.gid = 0
                        tarinfo.uname = "root"
                        tarinfo.gname = "root"
                        if "bin/" in rel_path:
                            tarinfo.mode = 0o755
                        elif "sudoers.d" in rel_path:
                            tarinfo.mode = 0o440
                        else:
                            tarinfo.mode = 0o644
                        with open(full_path, "rb") as fp:
                            tar.addfile(tarinfo, fp)
    data_bytes = data_buf.getvalue()

    with open(output_deb, "wb") as deb:
        deb.write(b"!<arch>\n")
        
        # 1. debian-binary
        deb.write(build_ar_header("debian-binary", len(debian_binary)))
        deb.write(debian_binary)
        if len(debian_binary) % 2 != 0:
            deb.write(b"\n")
            
        # 2. control.tar.gz
        deb.write(build_ar_header("control.tar.gz", len(control_data)))
        deb.write(control_data)
        if len(control_data) % 2 != 0:
            deb.write(b"\n")
            
        # 3. data.tar.gz
        deb.write(build_ar_header("data.tar.gz", len(data_bytes)))
        deb.write(data_bytes)
        if len(data_bytes) % 2 != 0:
            deb.write(b"\n")

    print(f"Successfully built Debian package: {output_deb} ({os.path.getsize(output_deb)} bytes)")

if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    pkg_dir = os.path.join(script_dir, "laptop-keyboard-auto-toggle_1.0-1_all")
    out_file = os.path.join(script_dir, "..", "laptop-keyboard-auto-toggle_1.0-1_all.deb")
    build_deb(pkg_dir, out_file)
