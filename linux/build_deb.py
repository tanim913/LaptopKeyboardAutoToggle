import os
import tarfile
import io
import time

def build_ar_header(name, size, mode=0o100644, mtime=None):
    if mtime is None:
        mtime = int(time.time())
    
    # Standard POSIX / Debian AR header (exactly 60 bytes, space-padded ASCII)
    # Positions 0..15:   File name (16 chars)
    # Positions 16..27:  Modification timestamp (12 chars)
    # Positions 28..33:  Owner ID (6 chars)
    # Positions 34..39:  Group ID (6 chars)
    # Positions 40..47:  File mode (8 chars octal)
    # Positions 48..57:  File size (10 chars decimal)
    # Positions 58..59:  Ending trailer `\n (2 chars)
    h_name = name.ljust(16)
    h_mtime = str(mtime).ljust(12)
    h_uid = "0".ljust(6)
    h_gid = "0".ljust(6)
    h_mode = f"{mode:o}".ljust(8)
    h_size = str(size).ljust(10)
    magic = "`\n"
    
    header_str = h_name + h_mtime + h_uid + h_gid + h_mode + h_size + magic
    header_bytes = header_str.encode("ascii")
    assert len(header_bytes) == 60, f"AR header must be 60 bytes, got {len(header_bytes)}"
    return header_bytes

def make_tar_gz(source_dir, is_control=False):
    buf = io.BytesIO()
    with tarfile.open(fileobj=buf, mode="w:gz", format=tarfile.GNU_FORMAT) as tar:
        # First add root directory entry
        root_info = tarfile.TarInfo("./")
        root_info.type = tarfile.DIRTYPE
        root_info.mode = 0o755
        root_info.uid = 0
        root_info.gid = 0
        root_info.uname = "root"
        root_info.gname = "root"
        tar.addfile(root_info)

        added_dirs = set(["./"])

        for root, dirs, files in os.walk(source_dir):
            rel_root = os.path.relpath(root, source_dir).replace("\\", "/")
            if rel_root != ".":
                dir_arcname = f"./{rel_root}"
                if dir_arcname not in added_dirs:
                    d_info = tarfile.TarInfo(dir_arcname)
                    d_info.type = tarfile.DIRTYPE
                    d_info.mode = 0o755
                    d_info.uid = 0
                    d_info.gid = 0
                    d_info.uname = "root"
                    d_info.gname = "root"
                    tar.addfile(d_info)
                    added_dirs.add(dir_arcname)

            for f in files:
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, source_dir).replace("\\", "/")
                tar_name = f"./{rel_path}"

                tarinfo = tar.gettarinfo(full_path, arcname=tar_name)
                tarinfo.uid = 0
                tarinfo.gid = 0
                tarinfo.uname = "root"
                tarinfo.gname = "root"

                # Ensure strict executable and security permissions
                if is_control:
                    if f in ["postinst", "prerm", "preinst", "postrm"]:
                        tarinfo.mode = 0o755
                    else:
                        tarinfo.mode = 0o644
                else:
                    if "/bin/" in f"/{rel_path}":
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
    
    # 1. Build control.tar.gz
    control_data = make_tar_gz(control_dir, is_control=True)
    
    # 2. Build data.tar.gz
    data_buf = io.BytesIO()
    with tarfile.open(fileobj=data_buf, mode="w:gz", format=tarfile.GNU_FORMAT) as tar:
        # Add root directory
        root_info = tarfile.TarInfo("./")
        root_info.type = tarfile.DIRTYPE
        root_info.mode = 0o755
        root_info.uid = 0
        root_info.gid = 0
        root_info.uname = "root"
        root_info.gname = "root"
        tar.addfile(root_info)

        added_dirs = set(["./"])

        for item in ["etc", "usr"]:
            item_path = os.path.join(pkg_root, item)
            if os.path.exists(item_path):
                for root, dirs, files in os.walk(item_path):
                    rel_root = os.path.relpath(root, pkg_root).replace("\\", "/")
                    dir_arcname = f"./{rel_root}"
                    if dir_arcname not in added_dirs:
                        d_info = tarfile.TarInfo(dir_arcname)
                        d_info.type = tarfile.DIRTYPE
                        d_info.mode = 0o755
                        d_info.uid = 0
                        d_info.gid = 0
                        d_info.uname = "root"
                        d_info.gname = "root"
                        tar.addfile(d_info)
                        added_dirs.add(dir_arcname)

                    for f in files:
                        full_path = os.path.join(root, f)
                        rel_path = os.path.relpath(full_path, pkg_root).replace("\\", "/")
                        tarinfo = tar.gettarinfo(full_path, arcname=f"./{rel_path}")
                        tarinfo.uid = 0
                        tarinfo.gid = 0
                        tarinfo.uname = "root"
                        tarinfo.gname = "root"

                        if "/bin/" in f"/{rel_path}":
                            tarinfo.mode = 0o755
                        elif "sudoers.d" in rel_path:
                            tarinfo.mode = 0o440
                        else:
                            tarinfo.mode = 0o644

                        with open(full_path, "rb") as fp:
                            tar.addfile(tarinfo, fp)
    data_bytes = data_buf.getvalue()

    # 3. Assemble Debian ar archive
    with open(output_deb, "wb") as deb:
        deb.write(b"!<arch>\n")
        
        # 3.1 debian-binary
        deb.write(build_ar_header("debian-binary", len(debian_binary)))
        deb.write(debian_binary)
        if len(debian_binary) % 2 != 0:
            deb.write(b"\n")
            
        # 3.2 control.tar.gz
        deb.write(build_ar_header("control.tar.gz", len(control_data)))
        deb.write(control_data)
        if len(control_data) % 2 != 0:
            deb.write(b"\n")
            
        # 3.3 data.tar.gz
        deb.write(build_ar_header("data.tar.gz", len(data_bytes)))
        deb.write(data_bytes)
        if len(data_bytes) % 2 != 0:
            deb.write(b"\n")

    print(f"Successfully built Debian package: {output_deb} ({os.path.getsize(output_deb)} bytes)")

if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    pkg_dir = os.path.join(script_dir, "package")
    with open(os.path.join(pkg_dir, "DEBIAN", "control")) as f:
        version = next(line.split(":", 1)[1].strip() for line in f if line.startswith("Version:"))
    out_file = os.path.join(script_dir, "..", f"laptop-keyboard-auto-toggle_{version}_all.deb")
    build_deb(pkg_dir, out_file)
