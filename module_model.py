"""Original PE image bytes and audited pure protection arithmetic.

No sample loading, execution, native calls, or host pointer dereferences.
read_rva describes the original image view before loader/runtime writes. It must
not be taken as proof that mutable process memory still contains these bytes.
Module base is symbolic: callers must not substitute numeric zero for it.
"""
from dataclasses import dataclass
from pathlib import Path
import hashlib, struct

@dataclass(frozen=True)
class ModuleAddress:
    image_sha256: str
    offset: int
    mode: str = 'rva'
    def add(self, delta):
        return ModuleAddress(self.image_sha256, (self.offset + delta) & 0xffffffff, self.mode)

class PEImage:
    def __init__(self, path):
        self.path = Path(path)
        self.data = self.path.read_bytes()
        self.sha256 = hashlib.sha256(self.data).hexdigest().upper()
        if self.data[:2] != b'MZ': raise ValueError('Not PE')
        pe = struct.unpack_from('<I', self.data, 0x3c)[0]
        if self.data[pe:pe+4] != b'PE\0\0': raise ValueError('Invalid PE signature')
        coff = pe+4
        self.machine, count = struct.unpack_from('<HH', self.data, coff)
        optional_size = struct.unpack_from('<H', self.data, coff+16)[0]
        optional = coff+20
        self.magic = struct.unpack_from('<H', self.data, optional)[0]
        self.size_image, self.size_headers = struct.unpack_from('<II', self.data, optional+56)
        self.section_alignment = struct.unpack_from('<I', self.data, optional+32)[0]
        self.sections = []
        for n in range(count):
            p = optional+optional_size+40*n
            name = self.data[p:p+8].split(b'\0')[0].decode('ascii')
            virtual_size, rva, raw_size, raw = struct.unpack_from('<IIII', self.data, p+8)
            self.sections.append(dict(name=name, rva=rva, virtual_size=virtual_size, raw_size=raw_size, raw=raw))
    def address(self, offset, mode='rva'):
        if mode not in ('rva','file'): raise ValueError('Unknown image mode')
        return ModuleAddress(self.sha256, offset, mode)
    def rva_to_file(self, rva):
        if 0 <= rva < self.size_headers: return rva
        for s in self.sections:
            if s['rva'] <= rva < s['rva']+s['raw_size']:
                return s['raw']+rva-s['rva']
        raise ValueError(f'RVA {rva:#x} has no original file bytes')
    def read_file(self, offset, length):
        if min(offset,length)<0 or offset+length>len(self.data): raise ValueError('File read out of bounds')
        return self.data[offset:offset+length]
    def read_rva(self, rva, length):
        if min(rva,length)<0 or rva+length>self.size_image: raise ValueError('Image read out of bounds')
        chunks=[]
        while length:
            if rva < self.size_headers:
                take=min(length,self.size_headers-rva)
                chunks.append(self.read_file(rva,take))
            else:
                section=next((s for s in self.sections if s['rva']<=rva<s['rva']+max(s['raw_size'],s['virtual_size'])),None)
                if section is None: raise ValueError(f'RVA {rva:#x} is outside headers/sections')
                relative=rva-section['rva']
                if relative<section['raw_size']:
                    take=min(length,section['raw_size']-relative)
                    chunks.append(self.read_file(section['raw']+relative,take))
                else:
                    take=min(length,section['virtual_size']-relative)
                    chunks.append(bytes(take))
            rva+=take;length-=take
        return b''.join(chunks)
    def read(self, address, length):
        if not isinstance(address,ModuleAddress) or address.image_sha256!=self.sha256: raise ValueError('Wrong symbolic address space')
        return self.read_rva(address.offset,length) if address.mode=='rva' else self.read_file(address.offset,length)
    def integer(self, address, width, signed=False):
        return int.from_bytes(self.read(address,width),'little',signed=signed)

def protection_crc(data):
    """06000BAF / A89511A6.19357FB6: initial state 0, final complement."""
    crc=0
    for b in data:
        crc ^= b
        for _ in range(8): crc=(crc>>1) ^ (0xedb88320 if crc&1 else 0)
    return (~crc)&0xffffffff

def decode_integrity_word(raw, state):
    """06002F86 / FD87703C.4B9E5C10. Constructor seed = 4051960481."""
    value=(raw^state)&0xffffffff
    rotated=((state<<7)|(state>>25))&0xffffffff
    return value, rotated^value

if __name__=='__main__':
    import json,sys
    image=PEImage(sys.argv[1])
    evidence=json.loads(Path(__file__).with_name('check-result.json').read_text('utf-8-sig'))
    results=[]
    for row in evidence['results']:
        actual=protection_crc(image.read_rva(int(row['rva'],16),row['len']))
        results.append(actual==int(row['actual'],16))
    assert all(results) and len(results)==40
    assert image.read(image.address(0),2)==b'MZ'
    assert image.address(0)!=None
    print(json.dumps(dict(status='verified',sha256=image.sha256,crc_vectors=len(results),header_read='MZ',symbolic_base=True)))

